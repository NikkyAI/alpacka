using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.Lib;
using Alpacka.Lib.Config;
using Alpacka.Lib.Mods;
using Alpacka.Lib.Net;

namespace Alpacka.CLI.Commands
{
    // TODO: Just a test command for now?
    public class CommandBuild : CommandLineApplication
    {
        public CommandBuild()
        {
            Name = "build";
            Description = "Builds the current alpacka pack";
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                // TODO: Find root directory with pack config file.
                var directory = Directory.GetCurrentDirectory();
                
                var config = ModpackConfig.LoadYAML(directory);
                try {
                    var build = await Build(config);
                    build.SaveJSON(directory, pretty: true);
                } catch (DownloaderException ex) {
                    Console.WriteLine(ex.Message);
                    return 1;
                }
                
                return 0;
            });
        }
        
        public static async Task<ModpackBuild> Build(ModpackConfig config)
        {
            using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using (var downloader = new ModpackDownloader(modsCache))
                return await downloader.Resolve(config);
        }
    }
}
