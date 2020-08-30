using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

using Amazon.S3;

using YamlDotNet.RepresentationModel;

namespace Cythral.CloudFormation.BuildTasks
{
    public class PackageTemplate : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string TemplateFile { get; set; }

        [Required]
        public string PackageBucket { get; set; }

        public string? OutputTemplateFile { get; set; }

        public override bool Execute()
        {
            Task.WaitAll(new Task[] { Package() });
            return true;
        }

        public async Task<string> Package()
        {
            var template = await GetTemplateFileContents();
            var templateReader = new StringReader(template);
            var yamlStream = new YamlStream();

            var s3Client = new AmazonS3Client();
            var uploader = new S3Uploader(s3Client, PackageBucket);

            var templateDirectory = Path.GetDirectoryName(TemplateFile);
            var packager = new TemplatePackager(templateDirectory!, PackageBucket, templateReader, yamlStream, uploader);

            var packagedFile = await packager.Package();

            if (OutputTemplateFile != null)
            {
                await File.WriteAllTextAsync(OutputTemplateFile, packagedFile);
            }

            return packagedFile;
        }

        private async Task<string> GetTemplateFileContents()
        {
            return await File.ReadAllTextAsync(TemplateFile);
        }
    }
}