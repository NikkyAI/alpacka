using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Curse;
using GitMC.Lib.Mods;
using GitMC.Lib.Net;

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
        
        public static async Task<ModpackBuild> Build(ModpackConfig config)
        {
            using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using (var downloader = new ModpackDownloader(modsCache)
                    .WithSourceHandler(new ModSourceCurse())
                    .WithSourceHandler(new ModSourceURL()))
                return await downloader.Resolve(config);
        }
    }
}
