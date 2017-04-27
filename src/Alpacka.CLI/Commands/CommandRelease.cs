// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using Microsoft.Extensions.CommandLineUtils;
// using LibGit2Sharp;
// using Alpacka.Lib;
// using Alpacka.Lib.Mods;
// using Alpacka.Lib.Pack;

// namespace Alpacka.CLI.Commands
// {
//     public class CommandRelease : CommandLineApplication
//     {
//         public CommandRelease()
//         {
//             Name = "release";
//             //TODO: description
//             Description = "Releases the current alpacka pack";
            
//             var argVersion = Argument("[version]",
//                 "Version that is released, defaults to current. Cannot be used with -i | --increase");
                
//             var optIncrease = Option("-i | --increase",
//                 "Increase the pack version [patch, minor, major] Cannot be used with [version]", CommandOptionType.SingleValue);
            
//             var optNoPush = Option("--no-push",
//                 "disable automatically pushing the release. Pushes branch and tag to 'origin'. Does NOT push other tags", CommandOptionType.NoValue);
            
//             var optNoCommit = Option("--no-commit",
//                 "Do not create a release commit, uses the last commit on the branch to tag as release", CommandOptionType.NoValue);
            
//             var optBuild = Option("-b | --build",
//                 "runs 'build' and generates the packbuild.json, needs to create a release commit", CommandOptionType.NoValue);
            
//             var optDirectory = Option("-d | --directory",
//                 "Sets the directory of the pack to release", CommandOptionType.SingleValue);
                
//             HelpOption("-? | -h | --help");
            
//             OnExecute(async () => {
//                 var instancePath = optDirectory.HasValue()
//                     ? Path.GetFullPath(optDirectory.Value())
//                     : Directory.GetCurrentDirectory();
                    
//                 if (!string.IsNullOrEmpty(argVersion.Value) && optIncrease.HasValue()) {
//                     Console.WriteLine("ERROR: version number and --increase set at the same time");
//                     ShowHelp();
//                     return 1;
//                 }
                
//                 if (string.IsNullOrEmpty(argVersion.Value) && !optIncrease.HasValue()) {
//                     Console.WriteLine("ERROR: set either version number or --increase");
//                     ShowHelp();
//                     return 0;
//                 }
                
//                 using (var repo = new Repository(instancePath)) {
//                     // check if we are on the default branch
//                     //TODO: add a flag for ignoring this
//                     var remoteHeadRef = repo.Refs["refs/remotes/origin/HEAD"];
//                     bool isDefaultBranch = repo.Head.TrackedBranch?.CanonicalName == remoteHeadRef.TargetIdentifier;
//                     if (!isDefaultBranch) {
//                         Console.WriteLine("WARNING: currently not on the default branch, do you really want to make a release?");
//                         return 1;
//                     }
                    
//                     // check for changed files
//                     var changedFiles = repo.RetrieveStatus().Where(f => f.State != LibGit2Sharp.FileStatus.Ignored );
//                     if (changedFiles.Count() != 0) {
//                         Console.WriteLine("ERROR: commit, stash, ignore or discard changes:");
//                         foreach (var f in changedFiles)
//                         {
//                             Console.WriteLine($"[ { f.State } ] > { f.FilePath }");
//                         }
//                         return 1;
//                     }
                    
//                     // get highest version
//                     var allTagVersions = repo.Tags.Select(t => {
//                         var vString = t.FriendlyName;
//                         System.Version v = null;
//                         if (!string.IsNullOrEmpty(vString) && vString[0] == 'v')
//                             System.Version.TryParse(vString.Substring(1), out v);
//                         return new { Tag = t, Version = v };
//                     }).OrderByDescending( a => a.Version);
//                     var lastVersion = allTagVersions.FirstOrDefault()?.Version;
                    
//                     // set default 
//                     if (lastVersion == null) lastVersion = new System.Version(0, 0, 0);
                    
//                     Debug.WriteLine($"last tag is version { lastVersion.ToString() }");
                    
//                     var versionString = argVersion.Value;
                    
//                     System.Version version = null;
//                     if(!string.IsNullOrEmpty(versionString)) {
//                         if (!System.Version.TryParse(versionString, out version)) {
//                             Debug.WriteLine($"failed to parse '{ versionString }' as version");
//                             return 1;
//                         }
//                     } else {
//                         if (optIncrease.HasValue()) {
//                             SemanticVersion increaseVersion;
//                             if (!Enum.TryParse(optIncrease.Value() ?? "patch", true, out increaseVersion)) {
//                                 Console.WriteLine("ERROR: parsing SemanticVersion failed");
//                             }
//                             version = lastVersion;
//                             int major = version.Major;
//                             int minor = version.Minor;
//                             int patch = version.Build;
//                             switch(increaseVersion) {
//                                 case SemanticVersion.Major:
//                                     major++;
//                                     minor = 0;
//                                     patch = 0;
//                                     break;
//                                 case SemanticVersion.Minor:
//                                     minor++;
//                                     patch = 0;
//                                     break;
//                                 case SemanticVersion.Patch:
//                                     patch++;
//                                     break;
//                                 default:
//                                     Console.WriteLine("ERROR unexpected SemanticVersion");
//                                     return 1;
//                             }
//                             version = new System.Version(major, minor, patch);
//                         } else {
                            
//                             return 1;
//                         }
//                     }
                    
//                     if(version < lastVersion) {
//                         //TODO: add flag to ignore this warning
//                         Console.WriteLine($"WARNING: version {version} is smaller than highest version {lastVersion}");
//                         return 1;
//                     }
                    
//                     var diffMajor = version.Major - lastVersion.Major;
//                     var diffMinor = version.Minor - lastVersion.Minor;
//                     var diffPatch = version.Build - lastVersion.Build;
//                     if(diffMajor > 1 || diffMinor > 1 || diffPatch > 1) {
//                         if(diffMajor > 1) Console.WriteLine($"WARNING: version difference of { diffMajor } in Major");
//                         if(diffMinor > 1) Console.WriteLine($"WARNING: version difference of { diffMinor } in Minor");
//                         if(diffPatch > 1) Console.WriteLine($"WARNING: version difference of { diffPatch } in Patch");
//                         return 1;
//                     }
                    
//                     if (diffMajor == 1 && (diffMinor > 0 || diffPatch > 0)) {
//                         if(diffMinor > 0) Console.WriteLine($"WARNING: version difference of { diffMinor } in Minor while also increasing version of Major");
//                         if(diffPatch > 0) Console.WriteLine($"WARNING: version difference of { diffPatch } in Patch while also increasing version of Major");
//                         return 1;
//                     }
                    
//                     if (diffMinor == 1 && diffPatch > 0) {
//                         if(diffPatch > 0) Console.WriteLine($"WARNING: version difference of { diffPatch } in Patch while also increasing version of Minor");
//                         return 1;
//                     }
                    
//                     var buildVersion = version.ToString();
//                     var tagName = $"v{buildVersion}";
                    
//                     if (allTagVersions.Any( a => (a.Version?.ToString() ?? "") == buildVersion )) {
//                         Console.WriteLine($"ERROR: version tag 'v{buildVersion}' already exists");
//                         return 1;
//                     }
                    
//                     Console.WriteLine($"selected version { buildVersion }");
//                     using( var config = Configuration.BuildFrom(".") ) {
//                         if(!optNoCommit.HasValue()) {
//                             if (optBuild.HasValue()) {
//                                 var packConfig = ModpackConfig.LoadYAML(instancePath);
                                
//                                 //TODO: factor out into method in CommandBuild to reduce duplications
//                                 try {
//                                     var build = await CommandBuild.Build(packConfig);
                                    
//                                     //set pack version
//                                     build.PackVersion = buildVersion;
//                                     build.SaveJSON(instancePath, pretty: true);
//                                 } catch (DownloaderException ex) {
//                                     Console.WriteLine(ex.Message);
//                                     return 1;
//                                 }
                                
//                                 // Stage the build file
//                                 LibGit2Sharp.Commands.Stage(repo, Constants.PACK_BUILD_FILE, new StageOptions{IncludeIgnored = true});
//                             } else {
//                                 // change only the pack version
                                
//                                 // read json (or build from yaml if no json is found)
//                                 var build = CommandUpdate.GetBuild(instancePath).Result;
                                    
//                                 //set pack version
//                                 build.PackVersion = buildVersion;
                                
//                                 build.SaveJSON(instancePath, pretty: true);
                                
//                                 // Stage the build file
//                                 LibGit2Sharp.Commands.Stage(repo, Constants.PACK_BUILD_FILE, new StageOptions{IncludeIgnored = true});
//                             }
//                             // Create the committer's signature and commit
//                             //TODO: ask for and set git config if not set
//                             var name = config.GetValueOrDefault<string>("user.name", "nobody");
//                             var email = config.GetValueOrDefault<string>("user.email", "@example.com");
//                             Signature user = new Signature(name, email, DateTime.Now);
                            
//                             // Commit to the repository
//                             Commit commit = repo.Commit($"Release { buildVersion }", user, user);
//                         } else {
//                             if (optBuild.HasValue()) {
//                                 Console.WriteLine($"ERROR: cannot use --no-comit and --build at the same time. Duh.");
//                                 return 1; // illegal argument configuration
//                             }
//                         }
//                     }
                    
//                     // create tag
//                     Tag tag = repo.ApplyTag(tagName);
                    
//                     // push to remote
//                     //TODO: get username and password or make key based auth work
//                     if (!optNoPush.HasValue()) {
//                         void Push(string remote, string identifier)
//                         {
//                             Console.WriteLine($"git push { remote } { identifier }"); 
//                             var startInfo = new ProcessStartInfo {
//                                 FileName  = "git", // TODO: Allow specifiying java bin path?
//                                 Arguments = $"push { remote } { identifier }",
//                                 WorkingDirectory = instancePath
//                             };
//                             using (var process = Process.Start(startInfo)) {
//                                 process.WaitForExit();
//                                 // TODO: Proper error handling. (Redirect process output?)
//                                 if (process.ExitCode != 0) throw new Exception(
//                                     $"Failed to push { identifier } to { remote } (exit code { process.ExitCode })");
                                
//                                 Console.WriteLine($"git push { remote } to { identifier } finished"); 
//                             }
//                         }
//                         foreach(var r in repo.Network.Remotes.Where( r => r.Name == "origin" )) {
//                             Debug.WriteLine($"Remote: {r.Name}");
//                             Push(r.Name, tagName);
//                             Push(r.Name, repo.Head.FriendlyName);
//                         }
//                     } else {
//                         Console.WriteLine($"Don't forget to push branch '{ repo.Head.FriendlyName }' and tag '{ tag.FriendlyName }'");
//                     }
//                 }
//                 return 0;
//             });
//         }
        
//         public enum SemanticVersion 
//         {
//             Major,
//             Minor,
//             Patch
//         }
//     }
// }
