namespace Cythral.CloudFormation.BuildTasks.Models
{
    public class StackPolicyBody
    {
        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }

#pragma warning disable CA2225
        public static StackPolicyBody operator +(StackPolicyBody body, string value)
        {
            body.Value += value;
            return body;
        }
#pragma warning restore CA2225
    }
}