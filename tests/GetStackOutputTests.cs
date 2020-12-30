using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Cythral.CloudFormation.BuildTasks
{
    public class GetStackOutputTests
    {
        [Test, Auto]
        public async Task ShouldReturnTheStackOutput(
            string stackName,
            string outputName,
            string outputValue,
            [Frozen, Substitute] IAmazonCloudFormation cloudformationClient,
            [Target] GetStackOutput getStackOutput
        )
        {
            var output = new Output { OutputKey = outputName, OutputValue = outputValue };
            var stack = new Stack { Outputs = new List<Output> { output } };
            cloudformationClient.DescribeStacksAsync(Any<DescribeStacksRequest>()).Returns(new DescribeStacksResponse
            {
                Stacks = new List<Stack> { stack }
            });

            getStackOutput.StackName = stackName;
            getStackOutput.OutputName = outputName;
            getStackOutput.Execute();

            getStackOutput.OutputValue.Should().Be(outputValue);
            await cloudformationClient.Received().DescribeStacksAsync(Is<DescribeStacksRequest>(request =>
                request.StackName == stackName
            ));
        }

        [Test, Auto]
        public async Task ShouldReturnNull_WhenTheStackDoesntExist(
            string stackName,
            string outputName,
            [Frozen, Substitute] IAmazonCloudFormation cloudformationClient,
            [Target] GetStackOutput getStackOutput
        )
        {
            cloudformationClient.DescribeStacksAsync(Any<DescribeStacksRequest>()).Returns(new DescribeStacksResponse
            {
                Stacks = new List<Stack>()
            });

            getStackOutput.StackName = stackName;
            getStackOutput.OutputName = outputName;
            getStackOutput.Execute();

            getStackOutput.OutputValue.Should().Be(null);
            await cloudformationClient.Received().DescribeStacksAsync(Is<DescribeStacksRequest>(request =>
                request.StackName == stackName
            ));
        }

        [Test, Auto]
        public async Task ShouldReturnNull_WhenTheOutputDoesntExist(
            string stackName,
            string outputName,
            [Frozen, Substitute] IAmazonCloudFormation cloudformationClient,
            [Target] GetStackOutput getStackOutput
        )
        {
            var stack = new Stack { Outputs = new List<Output>() };
            cloudformationClient.DescribeStacksAsync(Any<DescribeStacksRequest>()).Returns(new DescribeStacksResponse
            {
                Stacks = new List<Stack> { stack }
            });

            getStackOutput.StackName = stackName;
            getStackOutput.OutputName = outputName;
            getStackOutput.Execute();

            getStackOutput.OutputValue.Should().Be(null);
            await cloudformationClient.Received().DescribeStacksAsync(Is<DescribeStacksRequest>(request =>
                request.StackName == stackName
            ));
        }
    }
}
