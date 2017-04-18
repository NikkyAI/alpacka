using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Alpacka.Lib;
using Alpacka.Lib.Config;
using Alpacka.Lib.Net;
using Alpacka.Lib.Instances;
using Alpacka.Lib.Instances.MultiMC;

namespace Alpacka.CLI.Commands
{
    public class CommandUpdate : CommandLineApplication
    {
        public CommandUpdate()
        {
            Name = "update";
            Description = "Updates an alpacka pack, downloading mods";
            
            /* TODO: Implement version argument.
            var argVersion = Argument("[version]",
                "Version to update to, can be a release version " +
                "('recommended', 'latest' or git tag) or any git " +
                "commit-ish (branch, commit, 'HEAD~1' etc.)"); */
            
            var optDirectory = Option("-d | --directory",
                "Sets the directory of the pack to update", CommandOptionType.SingleValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                // if (argVersion.Value == null)
                //     throw new NotImplementedException("List versions");
                
                var instancePath = optDirectory.HasValue()
                    ? Path.GetFullPath(optDirectory.Value())
                    : Directory.GetCurrentDirectory();
                
                // TODO: switch branches and stuff
                
                return await CommandUpdate.Execute(instancePath);
                // // read packbuild.json
                
                // ModpackVersion build = await GetBuild(directory);
                
                // //TODO: download mods
                
                // var name = build.Name; //TODO: clean away spaces and special characters
                // var prettyName = build.Name;
                // var mcVersion = build.MinecraftVersion;
                // var forgeVersion = build.ForgeVersion;
                
                // var forgeData = await ForgeVersionData.Download();
                // ForgeVersion forge = forgeData[forgeVersion];
                // var info = AlpackaInfo.Load(directory);
                
                // if(info.Type == InstallType.Server)
                // {
                //     var forgeFile = await ForgeInstaller.InstallServer(directory, build, forge);
                    
                //     Console.WriteLine($"start forge server by executing {forgeFile}");
                    
                //     var modsDir = Path.Combine(directory, Constants.MC_MODS_DIR);
                    
                //     await DownloadMods(build.Mods, modsDir);
                    
                //     // TODO: mabye later use ModpackDownloader
                //     // List<DownloadedMod> downloaded;
                //     // using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
                //     // using (var downloader = new ModpackDownloader(modsCache)
                //     //         .WithSourceHandler(new ModSourceURL()))
                //     //     downloaded = await downloader.Run(modpackVersion);
                        
                //     // foreach (var downloadedMod in downloaded.Where(d => d.Mod.Side.IsServer()))
                //     //     File.Copy(downloadedMod.File.Path, Path.Combine(modsDir, downloadedMod.File.FileName));
                //     return 0;
                // }
                
                // return 0;
            });
        }
        
        public static async Task<int> Execute(string directory, ModpackBuild build = null)
        {
            // TODO: Move this elsewhere.
            var handlers = new Dictionary<string, IInstanceHandler> {
                // TODO: Vanilla handler.
                { "server", new ServerHandler() },
                { "multimc", new MultiMCHandler(@"C:\D\games\minecraft\MultiMC") } // FIXME: !!
            };
            
            if (build == null) build = await GetBuild(directory);
            
            var safeName   = string.Join("_", build.Name.Split(Path.GetInvalidPathChars()));
            var prettyName = build.Name;
            var mcVersion  = build.MinecraftVersion;
            
            var forgeData    = await ForgeVersionData.Download();
            var forgeVersion = forgeData[build.ForgeVersion];
            
            var modsDir     = Path.Combine(directory, Constants.MC_MODS_DIR);
            var alpackaInfo = AlpackaInfo.Load(directory);
            
            if (alpackaInfo != null) {
                var instanceType = alpackaInfo.InstanceType.ToLower();
                IInstanceHandler instanceHandler;
                if (!handlers.TryGetValue(instanceType, out instanceHandler)) {
                    Console.WriteLine($"ERROR: No handler for type '{ instanceType }'");
                    return 1;
                }
                instanceHandler.Update(directory, null, build); // FIXME: oldPack?
            }
            
            Console.WriteLine("Downloading mods ...");
            await DownloadMods(build.Mods, Side.Client, modsDir);
            
            return 0;
        }
        
        public static async Task<ModpackBuild> GetBuild(string directory)
        {
            var packBuildPath = Path.Combine(directory, Constants.PACK_BUILD_FILE);
            return File.Exists(packBuildPath)
                ? JsonConvert.DeserializeObject<ModpackBuild>(File.ReadAllText(packBuildPath))
                : await CommandBuild.Build(ModpackConfig.LoadYAML(directory));
        }
        
        public static Task DownloadMods(List<EntryMod> modList, Side side, string modsDir) {
            if (Directory.Exists(modsDir))
                Directory.Delete(modsDir, true);
            Directory.CreateDirectory(modsDir);
            
            var mods = modList.Where(mod => (mod.Side & side) == side).ToList(); 
            using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using (var downloader = new FileDownloaderURL(fileCache))
                return Task.WhenAll(mods.Select(async mod => {
                    var file = await downloader.Download(mod.Source);
                    if ((mod.MD5 != null) && (mod.MD5 != file.MD5))
                        throw new Exception($"MD5: '{ mod.MD5 }' does not match downloaded file's MD5: '{ file.MD5 }' { mod.Name }");
                    File.Copy(file.Path, Path.Combine(modsDir, file.FileName), true);
                }));
        }
    }
}
