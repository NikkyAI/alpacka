using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Mods;

namespace GitMC.CLI.Commands
{
    // TODO: Just a test command for now?
    public class CommandBuild : CommandLineApplication
    {
        public CommandBuild()
        {
            Name = "build";
            Description = "Builds the current gitMC pack";
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                // TODO: Find root directory with pack config file.
                var directory = ".";
                
                var config = ModpackConfig.Load(directory);
                
                var downloader = new ModpackDownloader()
                    .WithSourceHandler(new ModSourceURL());
                var downloaded = await downloader.Run(config);
                
                var modsDir = Path.Combine(directory, Constants.MODS_DIR);
                if (Directory.Exists(modsDir))
                    Directory.Delete(modsDir, true);
                Directory.CreateDirectory(modsDir);
                
                foreach (var downloadedMod in downloaded) {
                    var fileName = downloadedMod.File.FileName
                        ?? $"{ downloadedMod.Mod.Name }-{ downloadedMod.Mod.Version }.jar";
                    File.Move(downloadedMod.File.Path, Path.Combine(modsDir, fileName));
                }
                
                var build = new ModpackBuild(config);
                build.Save(directory, pretty: true);
                
                return 0;
            });
        }
    }
}
