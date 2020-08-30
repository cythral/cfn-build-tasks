using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Cythral.CloudFormation.BuildTasks.Models;

using static System.Text.Json.JsonTokenType;

namespace Cythral.CloudFormation.BuildTasks.Converters
{
    public class StackPolicyBodyConverter : JsonConverter<StackPolicyBody>
    {
        public override StackPolicyBody Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var body = new StackPolicyBody { Value = "" };
            var originalDepth = reader.CurrentDepth;
            var previousType = reader.TokenType;

            while (reader.TokenType != EndObject || reader.CurrentDepth != originalDepth)
            {
                switch (reader.TokenType)
                {
                    case StartArray: body += "["; break;
                    case EndArray: body += "]"; break;
                    case StartObject: body += "{"; break;
                    case EndObject: body += "}"; break;
                    case JsonTokenType.String: body += $"\"{reader.GetString()}\""; break;
                    case Number: body += $"{reader.GetInt64()}"; break;
                    case Null: body += "null"; break;
                    case PropertyName:
                        if (previousType != StartObject && previousType != StartArray)
                        {
                            body += ",";
                        }

                        body += $"\"{reader.GetString()}\":";
                        break;

                    default: break;
                }

                previousType = reader.TokenType;
                reader.Read();
            }

            body += "}";
            return body;
        }

        public override void Write(Utf8JsonWriter writer, StackPolicyBody value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}