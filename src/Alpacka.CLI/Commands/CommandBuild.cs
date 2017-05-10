using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Pack.Config;

namespace Alpacka.CLI.Commands
{
    public class CommandBuild : CommandLineApplication
    {
        public CommandBuild()
        {
            Name = "build";
            Description = "Builds the current alpacka pack";
            
            HelpOption("-? | -h | --help");
            
            OnExecute(() => {
                // TODO: Find root directory with pack config file.
                var directory = Directory.GetCurrentDirectory();
                
                var config = ModpackConfig.LoadYAML(directory);
                try {
                    var build = Build(config).Result;
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
            using (var downloader = new ModpackDownloader())
                return await downloader.Resolve(config);
        }
    }
}
