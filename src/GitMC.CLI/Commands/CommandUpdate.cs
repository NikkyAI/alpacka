using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Mods;
using GitMC.Lib.Net;
using GitMC.Lib.Instances;

namespace GitMC.CLI.Commands
{
    public class CommandUpdate : CommandLineApplication
    {
        public CommandUpdate()
        {
            Name = "update";
            Description = "Update the current gitMC pack";
            
            var argVersion = Argument("[version]",
                "Version to update to, can be a release version (git tag), or any git commit-ish");
            
            var optDirectory = Option("-d | --directory",
                "Sets the pack directory", CommandOptionType.SingleValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                if (argVersion.Value == null)
                    throw new NotImplementedException("List versions");
                
                var directory = optDirectory.HasValue()
                    ? Path.GetFullPath(optDirectory.Value())
                    : Directory.GetCurrentDirectory();
                
                // TODO: switch branches and stuff
                
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
        
        public static async Task<int> Execute(string directory, ModpackBuild build = null)
        {
            if (build == null) build = await GetBuild(directory);
            
            var name       = string.Join("_", build.Name.Split(Path.GetInvalidPathChars()));
            var prettyName = build.Name;
            var mcVersion  = build.MinecraftVersion;
            
            var forgeData    = await ForgeVersionData.Download();
            var forgeVersion = forgeData[build.ForgeVersion];
            
            var modsDir   = Path.Combine(directory, Constants.MC_MODS_DIR);
            var gitMCInfo = GitMCInfo.Load(directory);
            switch (gitMCInfo.Type) {
                
                case InstallType.MultiMC:
                    var packInstanceFolder = Directory.GetParent(directory).FullName;
                    var instanceConfigPath = Path.Combine(packInstanceFolder, "instance.cfg");
                    if (!File.Exists(instanceConfigPath))
                        throw new Exception("Not a MultiMC instance");
                    
                    // update minecraft version in instance.cfg
                    var instanceCfg = File.ReadAllText(instanceConfigPath);
                    var intendedVersion = $"\nIntendedVersion={ mcVersion }";
                    if (!instanceCfg.Contains(intendedVersion)) {
                        instanceCfg += intendedVersion;
                        File.WriteAllText(instanceConfigPath, instanceCfg);
                    }
                    
                    // TODO: copy icon and set icon = gitm_{ name }
                    
                    Console.WriteLine($"Installing Forge { build.ForgeVersion } ...");
                    var installedForge = await ForgeInstaller.InstallMultiMC(packInstanceFolder, build);
                    
                    Console.WriteLine("Downloading mods ...");
                    await DownloadMods(build.Mods, Side.Client, modsDir);
                    break;
                
                case InstallType.Server:
                    Console.WriteLine("Downloading mods ...");
                    await DownloadMods(build.Mods, Side.Server, modsDir);
                    
                    Console.WriteLine($"Installing Forge { build.ForgeVersion } ...");
                    var forgeFile = await ForgeInstaller.InstallServer(directory, build, forgeVersion);
                    
                    Console.WriteLine($"Start forge server by executing { forgeFile }");
                    
                    // TODO: mabye later use ModpackDownloader
                    // List<DownloadedMod> downloaded;
                    // using (var modsCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
                    // using (var downloader = new ModpackDownloader(modsCache)
                    //         .WithSourceHandler(new ModSourceURL()))
                    //     downloaded = await downloader.Run(modpackVersion);
                        
                    // foreach (var downloadedMod in downloaded.Where(d => d.Mod.Side.IsServer()))
                    //     File.Copy(downloadedMod.File.Path, Path.Combine(modsDir, downloadedMod.File.FileName));
                    break;
                
                default:
                    throw new NotImplementedException();
                
            }
            
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
                        throw new DownloaderException($"MD5: '{ mod.MD5 }' does not match downloaded file's MD5: '{ file.MD5 }' { mod.Name }");
                    File.Copy(file.Path, Path.Combine(modsDir, file.FileName), true);
                }));
        }
    }
}
