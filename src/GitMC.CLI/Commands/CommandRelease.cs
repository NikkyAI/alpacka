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
                "Version that is released defaults to current.");
                
            var optIncrease = Option("-i | --increase",
                "Increase the pack version [patch, minor, major]", CommandOptionType.SingleValue);
            
            var optPush = Option("-p | --push",
                "automatically push the release", CommandOptionType.NoValue);
                
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                // TODO: Find root directory with pack config file.
                var directory = Directory.GetCurrentDirectory();
                
                if (!string.IsNullOrEmpty(argVersion.Value) && optIncrease.HasValue()) {
                    Console.WriteLine("ERROR: version number and --increasde set at the same time");
                    return 1;
                }
                
                var packConfig = ModpackConfig.LoadYAML(directory);
                var build  = await Build(packConfig);
                
                using (var repo = new Repository(directory)) {
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
                    if (!string.IsNullOrEmpty(versionString) && System.Version.TryParse(versionString, out version)) {
                        
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
                                    break;
                            }
                            version = new System.Version(major, minor, patch);
                        }
                    }
                    
                    //set pack version
                    build.PackVersion = version?.ToString() ?? versionString;
                    
                    if (allTagVersions.Any( a => (a.Version?.ToString() ?? "") == build.PackVersion )) {
                        Console.WriteLine("ERROR version tag already exists");
                        return 1;
                    }
                    
                    Console.WriteLine($"selected version { build.PackVersion }");
                    
                    build.SaveJSON(directory, pretty: true);
                    
                    // Stage the build file
                    LibGit2Sharp.Commands.Stage(repo, Constants.PACK_BUILD_FILE, new StageOptions{IncludeIgnored = true});
                    
                    Commit parentCommit;
                    using( var config = Configuration.BuildFrom(".")) {
                        // Create the committer's signature and commit
                        //TODO: ask for and set git config if not set
                        var name = config.GetValueOrDefault<string>("user.name", "nobody");
                        var email = config.GetValueOrDefault<string>("user.email", "@example.com");
                        Signature author = new Signature(name, email, DateTime.Now);
                        Signature committer = author;
                        // Commit to the repository
                        Commit commit = repo.Commit($"Release { build.PackVersion }", author, committer);
                        parentCommit = commit.Parents.First();
                    }
                    var tagName = $"v{version}";
                    // create tag
                    {
                        Tag t = repo.ApplyTag(tagName);
                    }
                    
                    // push to remote
                    if (optPush.HasValue()) {
                        foreach(var r in repo.Network.Remotes) {
                            Console.WriteLine($"r.Name");
                            var remote = r.Name;
                            var ret = await ThreadUtil.RunProcessAsync("git", $"push { remote } { tagName }");
                            Console.WriteLine($"git push { remote } { tagName } finished with exit code { ret }");
                        }
                    }
                    
                    repo.Reset(ResetMode.Hard, parentCommit);
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
