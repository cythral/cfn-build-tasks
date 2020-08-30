using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Amazon.CloudFormation.Model;

namespace Cythral.CloudFormation.BuildTasks.Converters
{
    public class ParameterConverter : JsonConverter<List<Parameter>>
    {
        public override List<Parameter> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<Parameter>();
            reader.Read();

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                try
                {
                    var key = reader.GetString();
                    reader.Read();

                    var value = reader.GetString();
                    reader.Read();

                    list.Add(new Parameter
                    {
                        ParameterKey = key,
                        ParameterValue = value
                    });
                }
#pragma warning disable CA1031
                catch (Exception)
                {
                    break;
                }
#pragma warning restore CA1031
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<Parameter> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("{");
            int i = 0;

            foreach (var parameter in value)
            {
                writer.WriteStringValue($"\"{parameter.ParameterKey}\":\"{parameter.ParameterValue}\"");

                if (i != value.Count - 1)
                {
                    writer.WriteStringValue(",");
                }

                writer.WriteStringValue("\n");
                i++;
            }

            writer.WriteStringValue("}");
        }
    }
}