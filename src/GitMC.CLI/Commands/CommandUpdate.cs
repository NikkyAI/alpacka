using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using GitMC.Lib;
using GitMC.Lib.Mods;
using GitMC.Lib.Config;
using GitMC.Lib.Net;

namespace GitMC.CLI.Commands
{
    public class CommandUpdate : CommandLineApplication
    {
        public CommandUpdate()
        {
            Name = "update";
            Description = "Update the packs";
            
            var argVersion = Argument("[version]",
                "Version to update to. Can be 'recommended' .. ");
                
            var optDirectory = Option("-d | --directory",
                "Sets the pack directory", CommandOptionType.SingleValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                if(string.IsNullOrEmpty(argVersion.Value))
                {
                    Console.WriteLine("list versions here and exit");
                }
                
                var directory = Directory.GetCurrentDirectory();
                if(optDirectory.HasValue()) {
                    directory = optDirectory.Value();
                }
                
                //TODO: switch branches and stuff
                
                
                return await CommandUpdate.Execute(directory);
                // // read packbuild.json
                
                // ModpackVersion build = await GetBuild(directory);
                
                // //TODO: download mods
                
                // var name = build.Name; //TODO: clean away spaces and special characters
                // var prettyName = build.Name;
                // var mcVersion = build.MinecraftVersion;
                // var forgeVersion = build.ForgeVersion;
                
                // var forgeData = await ForgeVersionData.Download();
                // ForgeVersion forge = forgeData[forgeVersion];
                // var info = GitMCInfo.Load(directory);
                
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
        
        public static async Task<int> Execute(string directory, ModpackVersion build = null)
        {
            if (build == null) build = await GetBuild(directory);
            
            var name = build.Name; //TODO: clean away spaces and special characters
            var prettyName = build.Name;
            var mcVersion = build.MinecraftVersion;
            var forgeVersion = build.ForgeVersion;
            
            var forgeData = await ForgeVersionData.Download();
            ForgeVersion forge = forgeData[forgeVersion];
            var info = GitMCInfo.Load(directory);
            
            if(info.Type == InstallType.Server)
            {
                var modsDir = Path.Combine(directory, Constants.MC_MODS_DIR);
                Console.WriteLine("Downloading mods");
                await DownloadMods(build.Mods, Side.Server, modsDir);
                
                var forgeFile = await ForgeInstaller.InstallServer(directory, build, forge);
                
                Console.WriteLine($"start forge server by executing {forgeFile}");
                
                
                // TODO: mabye later use ModpackDownloader
                // List<DownloadedMod> downloaded;
                // using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
                // using (var downloader = new ModpackDownloader(modsCache)
                //         .WithSourceHandler(new ModSourceURL()))
                //     downloaded = await downloader.Run(modpackVersion);
                    
                // foreach (var downloadedMod in downloaded.Where(d => d.Mod.Side.IsServer()))
                //     File.Copy(downloadedMod.File.Path, Path.Combine(modsDir, downloadedMod.File.FileName));
                return 0;
            }
            return 0;
        }
        
        public static async Task<ModpackVersion> GetBuild(string directory)
        {
            ModpackVersion build = null;
            var packBuildPath = Path.Combine(directory, Constants.PACK_BUILD_FILE);
            
            if(!File.Exists(packBuildPath))
            {
                var config = ModpackConfig.LoadYAML(directory);
                build  = await CommandBuild.Build(config);
            } else {
                var packBuildText = File.ReadAllText(packBuildPath);
                build = JsonConvert.DeserializeObject<ModpackVersion>(packBuildText);
            }
            return build;
        }
        
        public static Task DownloadMods(List<EntryMod> modList, Side side, string modsDir) {
            if (Directory.Exists(modsDir))
                        Directory.Delete(modsDir, true);
                    Directory.CreateDirectory(modsDir);
                    
            var mods = modList.Where(mod => (mod.Side & side) == side); 
            using(var fileCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using(var downloader = new FileDownloaderURL(fileCache))
            {  
                return Task.WhenAll(mods.Select(async (m) => 
                {
                    var f = await downloader.Download(m.Source);
                    if(!string.IsNullOrEmpty(m.MD5) && m.MD5 != f.MD5)
                        throw new DownloaderException($"MD5: '{m.MD5}' does not match donwloaded file's MD5: '{f.MD5}' {m.Name}");
                    File.Copy(f.Path, Path.Combine(modsDir, f.FileName), true);
                }));
            }
        }
    }
}
