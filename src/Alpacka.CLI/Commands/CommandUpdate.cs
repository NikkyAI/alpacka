using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using LibGit2Sharp;
using Alpacka.Lib;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Pack.Config;

namespace Alpacka.CLI.Commands
{
    public class CommandUpdate : CommandLineApplication
    {
        public CommandUpdate()
        {
            Name = "update";
            Description = "Updates an alpacka pack, downloading mods";
            
            // TODO: Implement version argument.
            var argVersion = Argument("[version]",
                "Version to update to, can be a release version " +
                "('recommended', 'latest' or git tag) or any git " +
                "commit-ish (branch, commit, 'HEAD~1' etc.)"); 
            
            var optDirectory = Option("-d | --directory",
                "Sets the directory of the pack to update", CommandOptionType.SingleValue);
            var optList = Option("-l | --list",
                "List all pack versions", CommandOptionType.NoValue);
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                var instancePath = optDirectory.HasValue()
                    ? Path.GetFullPath(optDirectory.Value())
                    : Directory.GetCurrentDirectory();
                    
                if(optList.HasValue()/* || argVersion.Value == null*/) {
                    return ListVersions(instancePath);
                }
                
                // switch branches and stuff
                using (var repo = new Repository(instancePath))
                {
                    // check for changed files
                    var changedFiles = repo.RetrieveStatus().Where(f => f.State != LibGit2Sharp.FileStatus.Ignored );
                    if (changedFiles.Count() != 0) {
                        Console.WriteLine("WARNING: commit, stash, ignore or discard changes:");
                        foreach (var f in changedFiles)
                        {
                            Console.WriteLine($"[{ f.State }] > { f.FilePath }");
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
                    bool isDefaultBranch = repo.Head.TrackedBranch?.CanonicalName == remoteHeadRef?.TargetIdentifier; //NOTE: remoteHeadRef can be null, if the remote repo has it not defined maybe because there is only one branch
                    Debug.WriteLine($"is default branch: {isDefaultBranch}");
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
                                Debug.WriteLine($"Version: {tagVersion.Version} Commit: {commit.Message}");
                                // checkout tag
                                LibGit2Sharp.Commands.Checkout(repo, commit/*, new CheckoutOptions{ CheckoutModifiers = CheckoutModifiers.Force }*/);
                            } else {
                                Console.WriteLine($"ERROR: Cannot find any release");
                                return 1;
                            }
                        } else {
                            // we are not on a tag => we are on a branch
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
                            if (newBranch == null) {
                                Console.WriteLine($"ERROR: cannot find branch '{argVersion.Value}'");
                                return 1;
                            }
                            if (newBranch.IsRemote) {
                                Console.WriteLine("is remote");
                                var name = argVersion.Value.Split('/')[1];
                                Console.WriteLine(name);
                                var trackingBranch = repo.CreateBranch(name, newBranch.Tip);
                                LibGit2Sharp.Commands.Checkout(repo, trackingBranch);
                                LibGit2Sharp.Repository.ListRemoteReferences("");
                            }
                            if (newBranch.IsTracking) {
                                Console.WriteLine("is tracking");
                                LibGit2Sharp.Commands.Checkout(repo, newBranch /*, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force }*/ );
                                
                            }
                            
                            // var remoteBranch = newBranch.TrackedBranch;
                            // //reset to tip of remote
                            // repo.Reset(ResetMode.Mixed, remoteBranch.Tip);
                        }
                    }
                }
                
                return await CommandUpdate.Execute(instancePath);
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
                Debug.WriteLine($"Tip: { repo.Head.FriendlyName } { tip.MessageShort } { tip }");
                Console.WriteLine("Branches:");
                foreach(Branch b in repo.Branches)//.Where(b => !b.IsRemote))
                {
                    var prefix = b.IsCurrentRepositoryHead ? "*" : " ";
                    if (b.TrackedBranch?.FriendlyName != null)
                        Console.WriteLine($"{ prefix } { b.FriendlyName } -> { b.TrackedBranch?.FriendlyName }");
                    else
                        Console.WriteLine($"{ prefix } { b.FriendlyName }");
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
            
            var alpackaInfo = AlpackaInfo.Load(directory);
            Side side = Side.Both;
            
            if (alpackaInfo != null) {
                var instanceHandler = AlpackaRegistry.InstanceHandlers[alpackaInfo.InstanceType];
                if (instanceHandler == null) {
                    Console.WriteLine($"ERROR: No handler for type '{ alpackaInfo.InstanceType }'");
                    return 1;
                }
            
                instanceHandler.Update(directory, null, build); // FIXME: oldPack?
                side = instanceHandler.Side;
            }
            
            Console.WriteLine("Downloading mods ...");
            await DownloadFiles(build.Mods, side, directory);
            
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
        
        public static async Task DownloadFiles(List<EntryMod> modList, Side side, string directory) {
            var mcDir = Path.Combine(directory, Constants.MC_DIR);
            Directory.CreateDirectory(mcDir);
            
            // TODO: Handle this without deleting the mods directory.
            var modsDir = Path.Combine(directory, Constants.MC_MODS_DIR);
            if (Directory.Exists(modsDir))
                Directory.Delete(modsDir, true);
            Directory.CreateDirectory(modsDir);
            
            var mods = modList.Where(mod => mod.Side == null || (mod.Side & side) == side).ToList(); 
            using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "mods")))
            using (var downloader = new FileDownloader(fileCache))
                await Task.WhenAll(mods.Select(async mod => {
                    var file = await downloader.Download(mod.Source);
                    if ((mod.MD5 != null) && (mod.MD5 != file.MD5))
                        throw new Exception($"MD5: '{ mod.MD5 }' does not match downloaded file's MD5: '{ file.MD5 }' { mod.Name }");
                    var path = Path.Combine(mcDir, mod.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.Copy(file.FullPath, path);
                }));
        }
    }
}
