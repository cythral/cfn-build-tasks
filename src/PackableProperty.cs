using System.Collections.Generic;

using YamlDotNet.RepresentationModel;

namespace Cythral.CloudFormation.BuildTasks
{
    public class PackableProperty
    {
        public PackableResource PackableResourceDefinition { get; set; }

        public KeyValuePair<YamlNode, YamlNode> Property { get; set; }

        public YamlMappingNode ResourcePropertiesNode { get; set; }

        public string Name { get; set; }
    }
}