using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Alpacka.Lib;
using Alpacka.Lib.Git;
using Alpacka.Lib.Instances.MultiMC;

namespace Alpacka.CLI.Commands
{
    public class CommandMultiMC : CommandLineApplication
    {
        public CommandMultiMC()
        {
            Name = "multimc";
            Description = "Install a alpacka pack into MultiMC";
            
            var argPackUrl = Argument("[url]",
                "pack url");
            
            var optDirectory = Option("-d | --directory",
                "multimc directory", CommandOptionType.SingleValue);
            var optForce = Option("-f | --force",
                "Override already installed pack", CommandOptionType.NoValue);
            // var argPack = Argument("-n | --name",
            //     "name ?", true);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                MultiMCConfig multiMCconfig = null;
                if(!optDirectory.HasValue())
                {
                    //TODO: config loading
                    var deserializer = new DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .Build();
                    var path = Path.Combine(Constants.ConfigPath, "multimc.yaml");
                    using (var reader = new StreamReader(File.OpenRead(path)))
                        multiMCconfig = deserializer.Deserialize<MultiMCConfig>(reader);
                }
                var directory = optDirectory.Value() ?? multiMCconfig.Directory; //TODO: find multimc directory from path or config
                var instanceListDir = Path.Combine(directory, "instances");
                
                if (!Directory.Exists(instanceListDir)) {
                    //TODO: We really need that logging stuffs.
                    Console.WriteLine($"ERROR: { directory } is not a MultiMC folder");
                    return 1;
                }
                var url = argPackUrl.Value;
                
                var tempDir = InstallUtil.Clone(url, directory);
                
                var build = await CommandUpdate.GetBuild(tempDir);
                
                var name = build.Name;
                var prettyName = build.Name;
                var mcVersion = build.MinecraftVersion;
                
                var instanceFolder = Path.Combine(instanceListDir, build.Name);
                
                if (Directory.Exists(instanceFolder)) {
                    // TODO: We really need that logging stuffs.
                    
                    Console.WriteLine($"ERROR: installing { url } failed");
                    Console.WriteLine($"ERROR: { build.Name } is already Installed");
                    
                    var dir = new DirectoryInfo(tempDir);
                    dir.ClearReadOnly();
                    dir.Delete(true);
                    
                    return -1;
                }
                
                Directory.CreateDirectory(instanceFolder);
                
                var mcDirectory = Path.Combine(instanceFolder, "minecraft");
                
                Directory.Move(tempDir, mcDirectory);
                
                var instanceCfg = new MultiMCInstance {
                    InstanceName = prettyName,
                    IntendedVersion = mcVersion, //is set later
                    Notes = build.Description
                };
                
                File.WriteAllText(Path.Combine(instanceFolder, "instance.cfg"), instanceCfg.ToString());
                
                //TODO: add instance to instance group
                
                var info = new AlpackaInfo { Type = InstallType.MultiMC };
                info.Save(mcDirectory);
                
                Console.WriteLine($"Installed pack {name} in { Path.GetFullPath(mcDirectory) }");
                
                return await CommandUpdate.Execute(mcDirectory, build);
                
                // await ForgeInstaller.InstallMultiMC(instanceFolder, build);
                // var modsDir = Path.Combine(mcDirectory, Constants.MC_MODS_DIR);
                // await CommandUpdate.DownloadMods(build.Mods, modsDir);
                
                // Console.WriteLine($"Downloaded Mods for pack {name} in { Path.GetFullPath(mcDirectory) }");
                
                // return 0;
            });
        }
        
        public class MultiMCConfig
        {
            public string Directory { get; set; }
        }
    }
}
