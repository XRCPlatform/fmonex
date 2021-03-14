using System;
using CommandLine;

namespace FreeMarketOne.ServerCore
{
    public class CommandLineOptions
    {
        [Option('c', "config", Required = false, HelpText="Path to config file")]
        public string ConfigFile { get; set; }
        
        [Option('d', "datadir", Required = false, HelpText = "Path to data directory.")]
        public string DataDir { get; set; }

        [Option('p', "password", Required = false, HelpText="Password for user")]
        public string Password { get; set; }

        public static void Parse(string[] args, Action<CommandLineOptions> action)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(action);
        }
    }
}