using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Net;
using GitMC.Lib.Instances;
using LibGit2Sharp;

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
            
            var optList = Option("-l | --list",
                "List all pack versions", CommandOptionType.NoValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                var directory = /*optDirectory.HasValue()
                    ? Path.GetFullPath(optDirectory.Value())
                    :*/ Directory.GetCurrentDirectory();
                
                if(optList.HasValue()/* || argVersion.Value == null*/) {
                    return ListVersions(directory);
                }
                
                // if(argVersion.Value == null) {
                //     return ListVersions(directory);
                // }
                
                // TODO: switch branches and stuff
                using (var repo = new Repository(directory))
                {
                    // check for changed files
                    if ((repo.RetrieveStatus()).Where(f => f.State != LibGit2Sharp.FileStatus.Ignored ).Count() != 0) {
                        Console.WriteLine("WARNING: commit, stash, ignore or discard changes:");
                        foreach (var f in repo.RetrieveStatus())
                        {
                            Console.WriteLine($"> { f.FilePath }");
                        }
                        return 1;
                    }
                    
                    // check if we are on a tag currently 
                    var allTagVersions = repo.Tags.Select(t => {
                        var vString = t.FriendlyName;
                        System.Version v = null;
                        if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
                            System.Version.TryParse(vString.Substring(1), out v);
                        return new { Tag = t, Version = v };
                    }).OrderByDescending( a => a.Version);
                    
                    Tag currentTag = null;
                    System.Version currentVersion = null;
                    
                    var tip = repo.Head.Tip;
                    foreach (var t in allTagVersions)
                    {
                        var target = (Commit) t.Tag.Target;
                        if(target == tip) {
                            
                            currentTag = t.Tag;
                            currentVersion = t.Version;
                            break;
                        }
                    }
                    
                    var remoteHeadRef = repo.Refs["refs/remotes/origin/HEAD"];
                    bool isDefaultBranch = repo.Head.TrackedBranch?.CanonicalName == remoteHeadRef.TargetIdentifier;
                    Console.WriteLine($"is default branch: {isDefaultBranch}");
                    if(argVersion.Value == null) {
                        
                        if(currentTag != null) {
                            //TODO: get latest release
                            var tagVersion = repo.Tags.Select(t => {
                                var vString = t.FriendlyName;
                                System.Version v = null;
                                if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
                                    System.Version.TryParse(vString.Substring(1), out v);
                                return new { Tag = t, Version = v };
                            }).OrderByDescending( a => a.Version).FirstOrDefault();
                            
                            if(tagVersion != null) {
                                var commit = (Commit)tagVersion.Tag.Target;
                                Console.WriteLine($"Version: {tagVersion.Version} Commit: {commit.Message}");
                                // checkout tag
                                LibGit2Sharp.Commands.Checkout(repo, commit/*, new CheckoutOptions{ CheckoutModifiers = CheckoutModifiers.Force }*/);
                            } else {
                                Console.WriteLine($"ERROR: Cannot find any release");
                                return 1;
                            }
                        } else {
                            // TODO: pull.. auth etc
                            string logMessage = "";
                            foreach (Remote remote in repo.Network.Remotes)
                            {
                                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                                LibGit2Sharp.Commands.Fetch(repo, remote.Name, refSpecs, null, logMessage);
                            }
                            var remoteBranch = repo.Branches[$"origin/{repo.Head.FriendlyName}"];
                            //reset to tip of remote
                            repo.Reset(ResetMode.Mixed, remoteBranch.Tip);
                        }
                    } else {
                        System.Version version = null;
                        
                        if(System.Version.TryParse(argVersion.Value, out version)) {
                            //get Tag
                            Console.WriteLine($"finding tag '{argVersion.Value}'");
                            var tagVersion = repo.Tags.Select(t => {
                                var vString = t.FriendlyName;
                                System.Version v = null;
                                if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
                                    System.Version.TryParse(vString.Substring(1), out v);
                                return new { Tag = t, Version = v };
                            }).OrderByDescending( a => a.Version).Where(a => a.Version == version).FirstOrDefault();
                            
                            if(tagVersion != null) {
                                var commit = (Commit)tagVersion.Tag.Target;
                                Console.WriteLine($"Version: {tagVersion.Version} Commit: {commit.Message}");
                                // checkout tag
                                LibGit2Sharp.Commands.Checkout(repo, commit/*, new CheckoutOptions{ CheckoutModifiers = CheckoutModifiers.Force }*/);
                            
                            } else {
                                Console.WriteLine($"ERROR: Cannot find tag for version '{version}'");
                                return 1;
                            }
                            
                            
                        } else {
                            // switch to branch
                            Console.WriteLine($"switching to branch '{argVersion.Value}'");
                            var newBranch = repo.Branches[argVersion.Value];
                            if(newBranch == null) {
                                Console.WriteLine($"ERROR: cannot find branch '{argVersion.Value}'");
                                return 1;
                            }
                            LibGit2Sharp.Commands.Checkout(repo, newBranch/*, new CheckoutOptions{ CheckoutModifiers = CheckoutModifiers.Force }*/);
                            // var remoteBranch = newBranch.TrackedBranch;
                            // //reset to tip of remote
                            // repo.Reset(ResetMode.Mixed, remoteBranch.Tip);
                        }
                    }
                }
                
                return await CommandUpdate.Execute(directory);
            });
        }
        
        public static int ListVersions(string directory) {
            using (var repo = new Repository(directory))
            {
                var allTagVersions = repo.Tags.Select(t => {
                    var vString = t.FriendlyName;
                    System.Version v = null;
                    if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
                        System.Version.TryParse(vString.Substring(1), out v);
                    return new { Tag = t, Version = v };
                }).OrderByDescending( a => a.Version);
                
                
                var tip = repo.Head.Tip;
                Console.WriteLine($"Tip: { repo.Head.FriendlyName } { tip.MessageShort } { tip }");
                Console.WriteLine("Branches:");
                foreach(Branch b in repo.Branches.Where(b => !b.IsRemote))
                {
                    var prefix = b.IsCurrentRepositoryHead ? "*" : " ";
                    Console.WriteLine($"{ prefix } { b.FriendlyName } -> { b.TrackedBranch?.FriendlyName ?? "(none)" }");
                }
                Console.WriteLine("Releases:");
                foreach (var t in allTagVersions)
                {
                    var target = (Commit) t.Tag.Target;
                    var prefix = target == tip ? "*" : " ";
                    Console.WriteLine($"{ prefix }{ t.Version }");
                }
            }
            
            return 0;
        }
        
        //TODO: rename to Install or similar
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
            Console.WriteLine($"packbuildpath: {packBuildPath}");
            var settings = new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver() };
            return File.Exists(packBuildPath)
                ? JsonConvert.DeserializeObject<ModpackBuild>(File.ReadAllText(packBuildPath), settings)
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
