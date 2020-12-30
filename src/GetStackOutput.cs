using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Microsoft.Build.Framework;

namespace Cythral.CloudFormation.BuildTasks
{
    public class GetStackOutput : Microsoft.Build.Utilities.Task
    {
        private readonly IAmazonCloudFormation cloudformationClient;

        public GetStackOutput(IAmazonCloudFormation cloudformationClient)
        {
            this.cloudformationClient = cloudformationClient;
        }

        public GetStackOutput() : this(new AmazonCloudFormationClient()) { }

        [Required]
        public string StackName { get; set; }

        [Required]
        public string OutputName { get; set; }

        [Output]
        public string? OutputValue { get; set; }

        public override bool Execute()
        {
            Run().GetAwaiter().GetResult();
            return true;
        }

        public async Task Run()
        {
            var response = await cloudformationClient.DescribeStacksAsync(new DescribeStacksRequest
            {
                StackName = StackName,
            });

            var stack = response.Stacks.ElementAtOrDefault(0);
            IEnumerable<Output> outputs = stack?.Outputs ?? (IEnumerable<Output>)Array.Empty<Output>();

            var query = from output in outputs
                        where output.OutputKey == OutputName
                        select output.OutputValue;

            OutputValue = query.FirstOrDefault();
        }
    }
}
