using System;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.CLI.Commands;

namespace Alpacka.CLI
{
    public class Program : CommandLineApplication
    {
        public static int Main(string[] args) => new Program().Run(args);
        
        public static readonly string Version = typeof(Program).GetTypeInfo().Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        
        public Program()
        {
            Name        = "alpacka";
            FullName    = "Alpacka CLI Utility";
            Description = "Minecraft modpack development with the power of Git";
            
            Commands.Add(new CommandInit());
            Commands.Add(new CommandBuild());
            Commands.Add(new CommandInstall());
            Commands.Add(new CommandUpdate());
            
            VersionOption("-v | --version", Version);
            HelpOption("-? | -h | --help");
            
            OnExecute(() => { ShowHelp(); return 0; });
        }
        
        public int Run(string[] args)
        {
            try { return Execute(args); }
            catch (CommandParsingException ex) {
                Console.WriteLine(ex.Message);
                ShowHelp();
                return 0;
            }
        }
    }
}
