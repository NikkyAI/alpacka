using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.Lib;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;

namespace Alpacka.CLI.Commands
{
    public class CommandInit : CommandLineApplication
    {
        public CommandInit()
        {
            Name = "init";
            Description = "Create and initialize a new alpacka pack";
            
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

                // TODO: Move this to a utility method. (In Alpacka.Lib?)
                var resourceStream = GetType().GetTypeInfo().Assembly
                    .GetManifestResourceStream("Alpacka.CLI.Resources.packconfig.default.yaml");
                string defaultConfig;
                using (var reader = new StreamReader(resourceStream))
                    defaultConfig = reader.ReadToEnd();
                
                var packName = optName.Value() ??
                    // If dictionary argument is set and it doesn't
                    // contain path separators, use it as pack name.
                    ((argDirectory.Value?.IndexOfAny(@"/\".ToCharArray()) == -1)
                        ? argDirectory.Value : "...");
                var packDesc = optDescription.Value() ?? "...";
                var authors = optAuthors.HasValue()
                    ? string.Join(", ", optAuthors.Values)
                    : Environment.GetEnvironmentVariable("USERNAME") ?? "...";
                
                var forgeData    = ForgeVersionData.Download().Result;
                var mcVersion    = forgeData.GetRecentMCVersion(Release.Recommended);
                var forgeVersion = forgeData.GetRecent(mcVersion, Release.Recommended)?.GetFullVersion();
                
                defaultConfig = Regex.Replace(defaultConfig, "{{(.+)}}", match => {
                    switch (match.Groups[1].Value.Trim()) {
                        case "NAME": return packName;
                        case "DESCRIPTION": return packDesc;
                        case "AUTHORS": return authors;
                        case "MC_VERSION": return mcVersion;
                        case "FORGE_VERSION": return forgeVersion;
                        default: return "...";
                    }
                });
                
                File.WriteAllText(configPathFile, defaultConfig);
                
                Console.WriteLine($"Created stub alpacka pack in { Path.GetFullPath(directory) }");
                
                return 0;
            });
        }
    }
}
