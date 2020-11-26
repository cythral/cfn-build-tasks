using System;
using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Microsoft.Build.Framework;

using Task = System.Threading.Tasks.Task;

namespace Cythral.CloudFormation.BuildTasks
{
    public class DeleteStack : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string StackName { get; set; }

        private IAmazonCloudFormation Client { get; set; } = new AmazonCloudFormationClient();

        public override bool Execute()
        {
            Task.WaitAll(new Task[] { Run() });
            return true;
        }

        private async Task Run()
        {
            await Client.DeleteStackAsync(new DeleteStackRequest
            {
                StackName = StackName,
            });

            string status;
            while ((status = await GetStackStatus()) != "DELETE_COMPLETE")
            {
                if (status == "DELETE_FAILED")
                {
                    throw new Exception("Stack failed to delete.");
                }

                await Task.Delay(10000);
                Console.WriteLine("Waiting for delete to complete....");
            }
        }

        private async Task<string> GetStackStatus()
        {
            var request = new DescribeStacksRequest { StackName = StackName };
            var response = await Client.DescribeStacksAsync(request);
            return response.Stacks[0].StackStatus.Value;
        }
    }
}
