using CommandLine;

namespace tomxyz.googleapi
{
    public class Options
    {
        [Option('r', "regex", Required = true, HelpText = "Regex matching events in calendar with group matching year and groups that you can address in target pattern. Sample: (.*) - birthday \\((.*)\\)")]
        public string Regex { get; set; }

        [Option('g', "group", Required = true, HelpText = "Index of group with birth year (start from 1)")]
        public int Group { get; set; }

        [Option('p', "pattern", Required = true, HelpText = "Pattern of target event name where '$number' is marking groups from regex_to_find and $Y stands for age.\r\n\tCharacter + is delimeter. Sample: $1+$Y+($2)")]
        public string Patttern { get; set; }

        [Option('y', "year", Required = false, HelpText = $"year that will be used for event listing. Current year is default)")]
        public int? Year { get; set; }
    }
}

