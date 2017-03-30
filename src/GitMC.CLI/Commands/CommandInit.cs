using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Net;

namespace GitMC.CLI.Commands
{
    public class CommandInit : CommandLineApplication
    {
        public CommandInit()
        {
            Name = "init";
            Description = "Create and initialize a new gitMC pack";
            
            var argDirectory = Argument("[directory]",
                "The directory to initialize the pack in. Created if necessary.");
            
            var optName = Option("-n | --name",
                "Sets the pack name", CommandOptionType.SingleValue);
            var optDescription = Option("-d | --description",
                "Sets the pack description", CommandOptionType.SingleValue);
            var optAuthors = Option("-a | --authors",
                "Sets the pack author(s)", CommandOptionType.MultipleValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(() => {
                var directory      = argDirectory.Value ?? ".";
                var configPathFile = Path.Combine(directory, Constants.PACK_CONFIG_FILE);
                
                if (File.Exists(configPathFile)) {
                    // TODO: We really need that logging stuffs.
                    Console.WriteLine($"ERROR: { Constants.PACK_CONFIG_FILE } already exists in the target directory.");
                    return 1;
                }
                
                Directory.CreateDirectory(directory);
                
                // TODO: Move this to a utility method. (In GitMC.Lib?)
                var resourceStream = GetType().GetTypeInfo().Assembly
                    .GetManifestResourceStream("GitMC.CLI.Resources.packconfig.default.yaml");
                string defaultConfig;
                using (var reader = new StreamReader(resourceStream))
                    defaultConfig = reader.ReadToEnd();
                
                var packName = optName.Value() ?? Path.GetDirectoryName(directory);
                var packDesc = optDescription.Value() ?? "...";
                var authors = optAuthors.HasValue()
                    ? string.Join(", ", optAuthors.Values)
                    : Environment.GetEnvironmentVariable("USERNAME") ?? "...";
                
                var forgeData = ForgeVersionData.Download().Result;
                var mcVersion = forgeData.GetRecentMCVersion(DefaultVersion.Recommended);
                
                defaultConfig = Regex.Replace(defaultConfig, @"{{\s*([^\s]+)\s*}}", match => {
                    switch (match.Groups[1].Value) {
                        case "NAME": return packName;
                        case "DESCRIPTION": return packDesc;
                        case "AUTHORS": return authors;
                        case "MC_VERSION": return mcVersion;
                        default: return "...";
                    }
                });
                
                File.WriteAllText(configPathFile, defaultConfig);
                
                Console.WriteLine($"Created stub gitMC pack in { Path.GetFullPath(directory) }");
                
                return 0;
            });
        }
    }
}
