using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Curse;
using GitMC.Lib.Mods;
using GitMC.Lib.Net;
using System.Threading.Tasks;

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
                var directory = Directory.GetCurrentDirectory();
                
                var config = ModpackConfig.LoadYAML(directory);
                var build  = await Build(config);
                
                build.SaveJSON(directory, pretty: true);
                
                return 0;
            });
        }
        
        public static async Task<ModpackVersion> Build(ModpackConfig config)
        {
            var build  = config.Clone();
            
            DefaultVersion forgeRecommendation;
            if (Enum.TryParse(build.ForgeVersion, true, out forgeRecommendation))
                build.ForgeVersion = (await ForgeVersionData.Download())
                    .GetRecent(build.MinecraftVersion, forgeRecommendation)
                    .GetFullVersion();
            
            // If any mod versions are not set, set them to the default now (recommended or latest).
            foreach (var mod in build.Mods) if (mod.Version == null)
                mod.Version = config.Defaults.Version.ToString().ToLowerInvariant();
            
            List<DownloadedMod> downloaded;
            using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using (var downloader = new ModpackDownloader(modsCache)
                    .WithSourceHandler(new ModSourceCurse())
                    .WithSourceHandler(new ModSourceURL()))
                downloaded = await downloader.Run(build);
            
            build.Mods = downloaded.Select(d => d.Mod).ToList();
            return build;
        }
    }
}
