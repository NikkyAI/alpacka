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
                
                var config = ModpackConfig.LoadYAML(directory);
                var build  = config.Clone();
                
                // If any mod versions are not set, set them to the default now (recommended or latest).
                foreach (var mod in build.Mods) if (mod.Version == null)
                    mod.Version = config.Defaults.Version.ToString().ToLowerInvariant();
                
                List<DownloadedMod> downloaded;
                using (var downloader = new ModpackDownloader()
                        .WithSourceHandler(new ModSourceURL()))
                    downloaded = await downloader.Run(build);
                
                var modsDir = Path.Combine(directory, Constants.MODS_DIR);
                if (Directory.Exists(modsDir))
                    Directory.Delete(modsDir, true);
                Directory.CreateDirectory(modsDir);
                
                foreach (var downloadedMod in downloaded) {
                    var fileName = downloadedMod.File.FileName
                        ?? $"{ downloadedMod.Mod.Name }-{ downloadedMod.Mod.Version }.jar";
                    File.Copy(downloadedMod.File.Path, Path.Combine(modsDir, fileName));
                }
                
                build.SaveJSON(directory, pretty: true);
                
                // TODO: This is ugly. Don't use static file cache?
                Constants.ModsCache.Save();
                
                return 0;
            });
        }
    }
}
