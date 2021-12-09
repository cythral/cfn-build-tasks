using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using YamlDotNet.RepresentationModel;

namespace Cythral.CloudFormation.BuildTasks
{
    public class TemplatePackager
    {
        private readonly string templateDirectory;
        private readonly string? packageManifestFile;
        private readonly StringReader templateReader;
        private readonly YamlStream yamlStream;
        private readonly S3Uploader uploader;
        private readonly string bucketName;
        private readonly string? prefix;
        private readonly Dictionary<string, string> filesUploaded = new();

        public TemplatePackager(string templateDirectory, string bucketName, StringReader templateReader, YamlStream yamlStream, S3Uploader uploader, string? prefix, string? packageManifestFile)
        {
            this.templateDirectory = templateDirectory;
            this.templateReader = templateReader;
            this.yamlStream = yamlStream;
            this.uploader = uploader;
            this.bucketName = bucketName;
            this.prefix = prefix;
            this.packageManifestFile = packageManifestFile;
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
                filesUploaded.TryGetValue(path, out var filename);

                if (filename == null)
                {
                    var fileToUpload = GetFileToUpload(path, forceZip);
                    var sum = Sha256sum(fileToUpload);

                    filename = prefix != null ? $"{prefix}/{sum}" : sum;
                    var task = uploader.Upload(fileToUpload, filename);
                    uploadTasks.Add(task);
                    filesUploaded.Add(path, filename);
                }

                var resourceDefinition = prop.PackableResourceDefinition;
                var propsNode = prop.ResourcePropertiesNode;

                if (resourceDefinition.BucketNameProperty != null && resourceDefinition.ObjectKeyProperty != null)
                {
                    var newPropDefinition = new YamlMappingNode { };
                    newPropDefinition.Add(resourceDefinition.BucketNameProperty, bucketName);
                    newPropDefinition.Add(resourceDefinition.ObjectKeyProperty, filename);
                    propsNode.Children[prop.Name] = newPropDefinition;
                }
                else
                {
                    var url = resourceDefinition.UseHttpsUrl
                        ? $"https://{bucketName}.s3.amazonaws.com/{filename}"
                        : $"s3://{bucketName}/{filename}";

                    propsNode.Children[prop.Name] = new YamlScalarNode(url);
                }
            }

            await Task.WhenAll(uploadTasks);

            var builder = new StringBuilder();
            using var writer = new StringWriter(builder);
            yamlStream.Save(writer, false);

            if (packageManifestFile != null)
            {
                var manifestRepresentation = new
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    Files = filesUploaded,
                };

                var manifestContents = JsonSerializer.Serialize(manifestRepresentation);
                await File.WriteAllTextAsync(packageManifestFile, manifestContents);
            }

            return builder.ToString();
        }

        private static string Sha256sum(string file)
        {
            using var SHA256 = SHA256Managed.Create();
            using var fileStream = File.OpenRead(file);
            var bytes = SHA256.ComputeHash(fileStream);
            var sum = Convert.ToBase64String(bytes);

            return sum
                .Replace('+', '-')
                .Replace('/', '_');
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

                using var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Update);

                foreach (var entry in zipArchive.Entries)
                {
                    entry.ExternalAttributes |= Convert.ToInt32("755", 8) << 16;
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
                var children = new List<KeyValuePair<YamlNode, YamlNode>>();
                children.AddRange(resources);

                doc.Children.TryGetValue("Metadata", out var metadata);

                if (metadata != null && metadata is YamlMappingNode metadataMappingNode)
                {
                    children.AddRange(metadataMappingNode);
                }

                foreach (var resource in children)
                {
                    var resourceNameNode = (YamlScalarNode)resource.Key;
                    var resourceName = resourceNameNode.Value;
                    var resourceNode = (YamlMappingNode)resource.Value;
                    resourceNode.Children.TryGetValue("Type", out var type);
                    var typeName = (type as YamlScalarNode)?.Value;
                    var props = (YamlNode)resourceNode;

                    if (type != null)
                    {
                        resourceNode.Children.TryGetValue("Properties", out props);

                        if (props == null)
                        {
                            continue;
                        }
                    }

                    typeName ??= resourceName;

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
