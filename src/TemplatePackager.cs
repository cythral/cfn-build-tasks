using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
// using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Cythral.CloudFormation.BuildTasks
{
    public class TemplatePackager
    {
        private readonly string templateDirectory;
        private readonly StringReader templateReader;
        private readonly YamlStream yamlStream;
        private readonly S3Uploader uploader;
        private readonly string bucketName;
        private readonly Dictionary<string, string> filesUploaded = new Dictionary<string, string>();

        public TemplatePackager(string templateDirectory, string bucketName, StringReader templateReader, YamlStream yamlStream, S3Uploader uploader)
        {
            this.templateDirectory = templateDirectory;
            this.templateReader = templateReader;
            this.yamlStream = yamlStream;
            this.uploader = uploader;
            this.bucketName = bucketName;
        }

        public async Task<string> Package()
        {
            yamlStream.Load(templateReader);
            var uploadTasks = new List<Task>();
            var packableProps = PackableProperties.ToList();

            foreach (var prop in packableProps)
            {
                var forceZip = prop.PackableResourceDefinition.ForceZip;
                var propValueNode = prop.Property.Value as YamlScalarNode;
                var propValue = propValueNode?.Value;

                if (propValue == null)
                {
                    continue;
                }

                var path = Path.IsPathFullyQualified(propValue) ? propValue : Path.Combine(templateDirectory, propValue);

                filesUploaded.TryGetValue(path, out var sum);

                if (sum == null)
                {
                    var fileToUpload = GetFileToUpload(path, forceZip);
                    sum = Sha256sum(fileToUpload);

                    var task = uploader.Upload(fileToUpload, sum);
                    uploadTasks.Add(task);
                    filesUploaded.Add(path, sum);
                }

                var resourceDefinition = prop.PackableResourceDefinition;
                var propsNode = prop.ResourcePropertiesNode;

                if (resourceDefinition.BucketNameProperty != null && resourceDefinition.ObjectKeyProperty != null)
                {
                    var newPropDefinition = new YamlMappingNode { };
                    newPropDefinition.Add(resourceDefinition.BucketNameProperty, bucketName);
                    newPropDefinition.Add(resourceDefinition.ObjectKeyProperty, sum);
                    propsNode.Children[prop.Name] = newPropDefinition;
                }
                else
                {
                    propsNode.Children[prop.Name] = new YamlScalarNode($"s3://{bucketName}/{sum}");
                }
            }

            await Task.WhenAll(uploadTasks);

            var builder = new StringBuilder();
            using var writer = new StringWriter(builder);
            yamlStream.Save(writer, false);

            return builder.ToString();
        }

        private static string Sha256sum(string file)
        {
            using var SHA256 = SHA256Managed.Create();
            using var fileStream = File.OpenRead(file);
            return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
        }

        private static string GetFileToUpload(string path, bool forceZip)
        {
            var isDirectory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            var directoryToZip = path;

            if (!isDirectory && forceZip)
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);

                var fileName = Path.GetFileName(path);
                File.Copy(path, tempDirectory + "/" + fileName);

                directoryToZip = tempDirectory;
            }

            if (isDirectory || forceZip)
            {
                var zipFileName = Path.GetTempFileName() + Path.GetRandomFileName();
                ZipFile.CreateFromDirectory(directoryToZip, zipFileName);

                using var zipFile = File.Open(zipFileName, FileMode.Open, FileAccess.ReadWrite);

                Console.WriteLine(zipFile.CanSeek);

                using var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Update);

                foreach (var entry in zipArchive.Entries)
                {
                    entry.ExternalAttributes |= Convert.ToInt32("664", 8) << 16;
                }

                return zipFileName;
            }

            return path;
        }

        public IEnumerable<PackableProperty> PackableProperties
        {
            get
            {
                var doc = (YamlMappingNode)yamlStream.Documents[0].RootNode;
                var resources = (YamlMappingNode)doc.Children["Resources"];

                foreach (var resource in resources)
                {
                    var resourceNameNode = (YamlScalarNode)resource.Key;
                    var resourceName = resourceNameNode.Value;
                    var resourceNode = (YamlMappingNode)resource.Value;
                    var type = (YamlScalarNode)resourceNode.Children["Type"];
                    var typeName = type.Value;

                    resourceNode.Children.TryGetValue("Properties", out var props);

                    if (props == null)
                    {
                        continue;
                    }

                    foreach (var prop in (YamlMappingNode)props)
                    {
                        var propNameNode = (YamlScalarNode)prop.Key;
                        var propName = propNameNode.Value;

                        PackableResource.Resources.TryGetValue((typeName!, propName!), out var def);

                        var propValue = prop.Value as YamlScalarNode;

                        if (def != null && propValue?.Value != null)
                        {
                            yield return new PackableProperty
                            {
                                PackableResourceDefinition = def,
                                Property = prop,
                                ResourcePropertiesNode = (YamlMappingNode)props,
                                Name = propName!
                            };
                        }
                    }
                }
            }
        }
    }
}