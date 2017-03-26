using System.IO;
using System.Collections.Generic;
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
                
                List<DownloadedMod> downloaded;
                using (var downloader = new ModpackDownloader()
                        .WithSourceHandler(new ModSourceURL()))
                    downloaded = await downloader.Run(config);
                
                var modsDir = Path.Combine(directory, Constants.MODS_DIR);
                if (Directory.Exists(modsDir))
                    Directory.Delete(modsDir, true);
                Directory.CreateDirectory(modsDir);
                
                foreach (var downloadedMod in downloaded) {
                    var fileName = downloadedMod.File.FileName
                        ?? $"{ downloadedMod.Mod.Name }-{ downloadedMod.Mod.Version }.jar";
                    File.Copy(downloadedMod.File.Path, Path.Combine(modsDir, fileName));
                }
                
                var build = new ModpackBuild(config);
                build.Save(directory, pretty: true);
                
                // TODO: This is ugly. Don't use static file cache?
                Constants.ModsCache.Save();
                
                return 0;
            });
        }
    }
}
