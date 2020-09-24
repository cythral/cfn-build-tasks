using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Cythral.CloudFormation.BuildTasks.Converters;
using Cythral.CloudFormation.BuildTasks.Models;

using Microsoft.Build.Framework;

using Task = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.BuildTasks
{
    public class Deploy : Microsoft.Build.Utilities.Task
    {

#pragma warning disable CA1812
        class Config
        {
            public List<Parameter> Parameters { get; set; }
            public List<Tag> Tags { get; set; }
            public StackPolicyBody StackPolicy { get; set; }
        }
#pragma warning disable CA1812

        [Required]
        public string StackName { get; set; }

        [Required]
        public string TemplateFile { get; set; }


        public bool Package { get; set; } = false;

        public string PackageBucket { get; set; } = "";

        public string? Prefix { get; set; }

        public string? PackageManifestFile { get; set; }

        public string ConfigFile { get; set; } = "";

        public string Capabilities { get; set; } = "";

        public string? RoleArn { get; set; }

        public IAmazonCloudFormation Client { get; set; } = new AmazonCloudFormationClient();

        public override bool Execute()
        {
            Task.WaitAll(new Task[] { Run() });
            return true;
        }

        private async Task Run()
        {
            var template = await GetTemplateFileContents();
            var config = await GetConfigFileContents();
            var stackExists = await StackExists();

            string status;
            try
            {
                if (!stackExists)
                {
                    var request = new CreateStackRequest
                    {
                        StackName = StackName,
                        Capabilities = Capabilities.Split(";").ToList(),
                        TemplateBody = template,
                        StackPolicyBody = config.StackPolicy?.ToString(),
                        Parameters = config.Parameters,
                        Tags = config.Tags
                    };

                    if (RoleArn != null)
                    {
                        request.RoleARN = RoleArn;
                    }

                    await Client.CreateStackAsync(request);
                }
                else
                {
                    var request = new UpdateStackRequest
                    {
                        StackName = StackName,
                        Capabilities = Capabilities.Split(";").ToList(),
                        TemplateBody = template,
                        StackPolicyBody = config.StackPolicy?.ToString(),
                        Parameters = config.Parameters,
                        Tags = config.Tags
                    };

                    if (RoleArn != null)
                    {
                        request.RoleARN = RoleArn;
                    }

                    await Client.UpdateStackAsync(request);
                }
            }
            catch (AmazonCloudFormationException e)
            {
                if (e.Message == "No updates are to be performed.")
                {
                    Console.WriteLine("Done.");
                    return;
                }
                else throw new AmazonCloudFormationException(e.Message);
            }

            Thread.Sleep(200);

            while ((status = await GetStackStatus()).EndsWith("_IN_PROGRESS"))
            {
                Thread.Sleep(10000);
                Console.WriteLine("Waiting for create/update to complete....");
            }


            if (status.Contains("ROLLBACK") || status.EndsWith("FAILED"))
            {
                throw new Exception("Deployment failed.  Check the stack logs.");
            }

            Console.WriteLine("Done.");
        }

        private async Task<Config> GetConfigFileContents()
        {
            if (!File.Exists(ConfigFile))
            {
                throw new Exception($"{ConfigFile} does not exist.");
            }

            using var stream = File.OpenRead(ConfigFile);
            var options = new JsonSerializerOptions();

            options.Converters.Add(new ParameterConverter());
            options.Converters.Add(new TagConverter());
            options.Converters.Add(new StackPolicyBodyConverter());

            return await JsonSerializer.DeserializeAsync<Config>(stream, options);
        }

        private async Task<string> GetTemplateFileContents()
        {
            if (Package)
            {
                var packageTask = new PackageTemplate
                {
                    TemplateFile = TemplateFile,
                    PackageBucket = PackageBucket,
                    PackageManifestFile = PackageManifestFile,
                    Prefix = Prefix
                };

                return await packageTask.Package();
            }

            return await File.ReadAllTextAsync(TemplateFile);
        }

        private async Task<bool> StackExists()
        {
#pragma warning disable CA1031
            try
            {
                var request = new DescribeStacksRequest { StackName = StackName };
                var response = await Client.DescribeStacksAsync(request);
                return response.Stacks.Any();
            }
            catch (Exception)
            {
                return false;
            }
#pragma warning restore CA1031
        }

        private async Task<string> GetStackStatus()
        {
            var request = new DescribeStacksRequest { StackName = StackName };
            var response = await Client.DescribeStacksAsync(request);
            return response.Stacks[0].StackStatus.Value;
        }
    }
}
