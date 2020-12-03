using System.Collections.Generic;
namespace Cythral.CloudFormation.BuildTasks
{
    public class PackableResource
    {
        private PackableResource()
        {

        }

        public bool PackNullProperty { get; set; } = false;

        public bool ForceZip { get; set; } = false;

        public string? BucketNameProperty { get; set; }

        public string? ObjectKeyProperty { get; set; }

        public string? VersionProperty { get; set; }

        public static Dictionary<(string, string), PackableResource> Resources { get; } = new Dictionary<(string, string), PackableResource>
        {
            [("AWS::Serverless::Function", "CodeUri")] = new PackableResource
            {
                ForceZip = true,
            },

            [("AWS::Serverless::Api", "DefinitionUri")] = new PackableResource
            {

            },

            [("AWS::AppSync::GraphQLSchema", "DefinitionS3Location")] = new PackableResource
            {

            },

            [("AWS::AppSync::Resolver", "RequestMappingTemplateS3Location")] = new PackableResource
            {

            },

            [("AWS::AppSync::Resolver", "ResponseMappingTemplateS3Location")] = new PackableResource
            {

            },

            [("AWS::AppSync::FunctionConfiguration", "RequestMappingTemplateS3Location")] = new PackableResource
            {

            },

            [("AWS::AppSync::FunctionConfiguration", "ResponseMappingTemplateS3Location")] = new PackableResource
            {

            },

            [("AWS::Lambda::Function", "Code")] = new PackableResource
            {
                BucketNameProperty = "S3Bucket",
                ObjectKeyProperty = "S3Key",
                VersionProperty = "S3ObjectVersion",
                ForceZip = true
            },

            [("AWS::ApiGateway::RestApi", "BodyS3Location")] = new PackableResource
            {
                BucketNameProperty = "Bucket",
                ObjectKeyProperty = "Key",
                VersionProperty = "Version",
            },

            [("AWS::ElasticBeanstalk::ApplicationVersion", "SourceBundle")] = new PackableResource
            {
                BucketNameProperty = "S3Bucket",
                ObjectKeyProperty = "S3Key",
            },

            [("AWS::Lambda::LayerVersion", "Content")] = new PackableResource
            {
                BucketNameProperty = "S3Bucket",
                ObjectKeyProperty = "S3Key",
                VersionProperty = "S3ObjectVersion",
                ForceZip = true,
            },

            [("AWS::Serverless::LayerVersion", "ContentUri")] = new PackableResource
            {
                ForceZip = true,
            },

            [("AWS::ServerlessRepo::Application", "ReadmeUrl")] = new PackableResource
            {

            },

            [("AWS::ServerlessRepo::Application", "LicenseUrl")] = new PackableResource
            {

            },

            [("AWS::StepFunctions::StateMachine", "DefinitionS3Location")] = new PackableResource
            {
                BucketNameProperty = "Bucket",
                ObjectKeyProperty = "Key",
                VersionProperty = "Version",
            },
        };
    }
}
