using System;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace Cythral.CloudFormation.BuildTasks
{
    public class UpdateCodeLocation : Task
    {

        [Required]
        public string TemplateFile { get; set; }

        [Required]
        public string Location { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(TemplateFile))
            {
                throw new Exception($"${TemplateFile} does not exist.");
            }

            var regex = new Regex("CodeUri:(.*)\n");
            var contents = File.ReadAllText(TemplateFile);
            var newContents = regex.Replace(contents, $"CodeUri: {Location}\n");


            File.WriteAllText(TemplateFile, newContents);
            return true;
        }

    }
}
