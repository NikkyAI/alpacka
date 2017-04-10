using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using LibGit2Sharp;
using GitMC.Lib;
using GitMC.Lib.Net;
using GitMC.Lib.Mods;
using GitMC.Lib.Util;
using GitMC.Lib.Curse;
using GitMC.Lib.Config;

namespace GitMC.CLI.Commands
{
    // TODO: Just a test command for now?
    public class CommandRelease : CommandLineApplication
    {
        public CommandRelease()
        {
            Name = "release";
            //TODO: description
            Description = "Releases the current gitMC pack";
            
            var argVersion = Argument("[version]",
                "Version that is released, defaults to current. Cannot be used with -i | --increase");
                
            var optIncrease = Option("-i | --increase",
                "Increase the pack version [patch, minor, major] Cannot be used with [version]", CommandOptionType.SingleValue);
            
            var optPush = Option("-p | --push",
                "automatically push the release. Pushes 'release' branch and tag to 'origin', also sets upstream for 'release' branch. Does NOT push other tags", CommandOptionType.NoValue);
                
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                // TODO: Find root directory with pack config file.
                var directory = Directory.GetCurrentDirectory();
                
                if (!string.IsNullOrEmpty(argVersion.Value) && optIncrease.HasValue()) {
                    Console.WriteLine("ERROR: version number and --increase set at the same time");
                    ShowHelp();
                    return 1;
                }
                
                if (string.IsNullOrEmpty(argVersion.Value) && !optIncrease.HasValue()) {
                    Console.WriteLine("ERROR: set either version number or --increase");
                    ShowHelp();
                    return 0;
                }
                
                using (var repo = new Repository(directory)) {
                    // check if we are on the default branch
                    //TODO: add a flag for ignoring this
                    var remoteHeadRef = repo.Refs["refs/remotes/origin/HEAD"];
                    bool isDefaultBranch = repo.Head.TrackedBranch?.CanonicalName == remoteHeadRef.TargetIdentifier;
                    if (!isDefaultBranch) {
                        Console.WriteLine("WARNING: currently on the default branch, do you really want to make a release?");
                        return 1;
                    }
                    
                    // check for changed files
                    if ((repo.RetrieveStatus()).Where(f => f.State != LibGit2Sharp.FileStatus.Ignored ).Count() != 0) {
                        Console.WriteLine("ERROR: commit, stash, ignore or discard changes:");
                        // foreach (var f in repo.RetrieveStatus())
                        // {
                        //     Console.WriteLine($"> { f.FilePath }");
                        // }
                        Console.WriteLine(repo.RetrieveStatus().ToPrettyJson());
                        return 1;
                    }
                    
                    // get highest version
                    var allTagVersions = repo.Tags.Select(t => {
                        var vString = t.FriendlyName;
                        System.Version v = null;
                        if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
                            System.Version.TryParse(vString.Substring(1), out v);
                        return new { Tag = t, Version = v };
                    }).OrderByDescending( a => a.Version);
                    var lastVersion = allTagVersions.FirstOrDefault()?.Version;
                    
                    // set default 
                    if (lastVersion == null) lastVersion = new System.Version(0, 0, 0);
                    
                    Console.WriteLine($"last tag is version { lastVersion.ToString() }");
                    
                    var versionString = argVersion.Value;
                    
                    System.Version version = null;
                    if(!string.IsNullOrEmpty(versionString)) {
                        if (!System.Version.TryParse(versionString, out version)) {
                            Console.WriteLine($"failed to parse '{ versionString }' as version");
                            return 1;
                        }
                    } else {
                        if (optIncrease.HasValue()) {
                            SemanticVersion increaseVersion;
                            if (!Enum.TryParse(optIncrease.Value() ?? "patch", true, out increaseVersion)) {
                                Console.WriteLine("ERROR: parsing SemanticVersion failed");
                            }
                            version = lastVersion;
                            int major = version.Major;
                            int minor = version.Minor;
                            int patch = version.Build;
                            switch(increaseVersion) {
                                case SemanticVersion.Major:
                                    major++;
                                    minor = 0;
                                    patch = 0;
                                    break;
                                case SemanticVersion.Minor:
                                    minor++;
                                    patch = 0;
                                    break;
                                case SemanticVersion.Patch:
                                    patch++;
                                    break;
                                default:
                                    Console.WriteLine("ERROR unexpected SemanticVersion");
                                    return 1;
                            }
                            version = new System.Version(major, minor, patch);
                        } else {
                            
                            return 1;
                        }
                    }
                    
                    if(version < lastVersion) {
                        //TODO: add flag to ignore this warning
                        Console.WriteLine($"WARNING: version {version} is smaller than highest version {lastVersion}");
                        return 1;
                    }
                    
                    var buildVersion = version.ToString();
                    var tagName = $"v{buildVersion}";
                    
                    if (allTagVersions.Any( a => (a.Version?.ToString() ?? "") == buildVersion )) {
                        Console.WriteLine($"ERROR: version tag 'v{buildVersion}' already exists");
                        return 1;
                    }
                    
                    Commit masterCommit = repo.Head.Tip;
                    
                    Console.WriteLine($"selected version { buildVersion }");
                    
                    //TODO: make build step optiona with --build
                    var packConfig = ModpackConfig.LoadYAML(directory);
                    var build  = await Build(packConfig);
                    
                    //set pack version
                    build.PackVersion = buildVersion; // version?.ToString() ?? versionString;
                    
                    using( var config = Configuration.BuildFrom(".") ) {
                        // Create the committer's signature and commit
                        //TODO: ask for and set git config if not set
                        var name = config.GetValueOrDefault<string>("user.name", "nobody");
                        var email = config.GetValueOrDefault<string>("user.email", "@example.com");
                        Signature user = new Signature(name, email, DateTime.Now);
                        
                        build.SaveJSON(directory, pretty: true);
                        
                        // Stage the build file
                        LibGit2Sharp.Commands.Stage(repo, Constants.PACK_BUILD_FILE, new StageOptions{IncludeIgnored = true});
                        
                        // Commit to the repository
                        Commit commit = repo.Commit($"Release { build.PackVersion }", user, user);
                    }
                    
                    // create tag
                    Tag tag = repo.ApplyTag(tagName);
                    
                    // push to remote
                    //TODO: get username and password or make key based auth work
                    if (optPush.HasValue()) {
                        foreach(var r in repo.Network.Remotes) {
                            Console.WriteLine($"r.Name");
                            var remote = r.Name;
                            var retTag = await ThreadUtil.RunProcessAsync("git", $"push { remote } { tagName }");
                            Console.WriteLine($"git push { remote } { tagName } finished with exit code { retTag }");
                            var retBranch = await ThreadUtil.RunProcessAsync("git", $"push { remote } { repo.Head.FriendlyName }");
                            Console.WriteLine($"git push { remote } { repo.Head.FriendlyName } finished with exit code { retBranch }");
                        }
                    } else {
                        Console.WriteLine($"Don't forget to push branch '{ repo.Head.FriendlyName }' and tag '{ tag.FriendlyName }'");
                    }
                }
                return 0;
            });
        }
        
        public enum SemanticVersion 
        {
            Major,
            Minor,
            Patch
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
