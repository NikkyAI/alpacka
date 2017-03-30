using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using GitMC.Lib;
using GitMC.Lib.Git;
using GitMC.Lib.Net;
using GitMC.Lib.Config;

namespace GitMC.CLI.Commands
{
    public class CommandServer : CommandLineApplication
    {
        public CommandServer()
        {
            Name = "server";
            Description = "install a gitmc pack on a forge server";
            
            var argPackUrl = Argument("[url]",
                "pack url");
            
            var optDirectory = Option("-d | --directory",
                "server directory?", CommandOptionType.SingleValue);
            // var argPack = Argument("-n | --name",
            //     "name ?", true);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () =>
            {
                var directory = optDirectory.Value() ?? Directory.GetCurrentDirectory();
                var url = argPackUrl.Value;

                var tempDir = InstallUtil.Clone(url, directory);
                
                ModpackVersion build = await CommandUpdate.GetBuild(tempDir);
                
                var instanceFolder = Path.Combine(directory, build.Name);

                if (Directory.Exists(instanceFolder))
                {
                    // TODO: We really need that logging stuffs.

                    Console.WriteLine($"ERROR: installing { url } failed");
                    Console.WriteLine($"ERROR: { build.Name } is already Installed");

                    var dir = new DirectoryInfo(tempDir);
                    dir.ClearReadOnly();
                    dir.Delete(true);

                    return -1;
                }

                Directory.Move(tempDir, instanceFolder);

                var prettyName = build.Name;
                var mcVersion = build.MinecraftVersion; //is set later (probably)

                var info = new GitMCInfo
                {
                    Type = InstallType.Server
                };
                info.Save(instanceFolder);

                Console.WriteLine($"Installed pack {build.Name} in { Path.GetFullPath(instanceFolder) }");
                
                return await CommandUpdate.Execute(instanceFolder, build);
                
                // var forgeData = await ForgeVersionData.Download();
                // ForgeVersion forge = forgeData[build.ForgeVersion];

                // await ForgeInstaller.InstallServer(instanceFolder, build, forge);
                // await CommandUpdate.DownloadMods(build.Mods, instanceFolder); 
            });
        }
    }
}
