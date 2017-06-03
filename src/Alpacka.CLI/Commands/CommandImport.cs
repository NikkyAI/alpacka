using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.Lib;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Pack.Config;
using Alpacka.Lib.Curse;
using Alpacka.Lib.Utility;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Alpacka.CLI.Commands
{
    public class CommandImport : CommandLineApplication
    {
        public CommandImport()
        {
            Name = "import";
            Description = "Create and initialize a new alpacka pack from a existing curse pack";
            
            var argType = Argument("type",
                "The type of instance can be 'Vanilla', 'Server' or 'MultiMC'");
                
            var argProject = Argument("project",
                "Project name of the curse modpack");
            
            var argFile = Argument("[file]",
                "Version or Filename");
            
            var optList = Option("-l | --list",
                "list available files", CommandOptionType.NoValue);
                
            var opMCVersion = Option("--mc",
                "specify minecraft version", CommandOptionType.SingleValue);
            
            //TODO: allow projectid fileid
            //TODO: allow projectname version
            //TODO: allow projectid version
            //TODO: allow projectname fileid
            
            //TODO: allow http://
            //TODO: allow file://
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                var instanceHandler = AlpackaRegistry.InstanceHandlers[argType.Value];
                var list = argType.Value.Equals("list", StringComparison.OrdinalIgnoreCase);
                if (instanceHandler == null && !list) {
                    Console.WriteLine($"ERROR: No handler for type '{ argType.Value }'");
                    return 1;
                }
                list = list || optList.HasValue();
                
                var isUrl = argProject.Value.StartsWith("https://") || argProject.Value.StartsWith("http://");
                var isFile = argProject.Value.StartsWith("file:");
                string url = "";
                
                Addon modpackProject = null;
                AddonFile modpackfile = null;
                int projectid = -1, fileid = -1;
                Release release = Release.Recommended;
                bool hasRelease = false;
                if (!isUrl && !isFile) {
                    if(int.TryParse(argProject.Value, out projectid)) {
                        modpackProject = await CurseMeta.Instance.GetAddon(projectid);
                        Console.WriteLine($"INFO: got project from id: '{ modpackProject.Name }'");
                    } else {
                        Console.WriteLine($"INFO: Project filter = '{ argProject.Value }'");
                        var projectList = await ProjectFeed.GetModPacks();
                        var projects = projectList.Data.Where(a => a.PackageType == PackageTypes.ModPack && a.Name.Trim().Contains(argProject.Value.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                        if(projects.Count == 0) {
                            Console.WriteLine($"WARNING: cannot find a modpack '{ argProject.Value }', try a more accurate search string or look up packs here https://cursemeta.dries007.net/search#modpacks");
                            return Program.Cleanup(-1);
                        } else if (projects.Count > 1) {
                            bool exactmatch = false;
                            foreach (var p in projects ) {
                                if(p.Name.Equals(argProject.Value, StringComparison.OrdinalIgnoreCase)) {
                                    exactmatch = true;
                                    projects = new List<Addon> { p };
                                }
                            }
                            if (!exactmatch) {
                                Console.WriteLine($"WARNING: found more than one modpack, try a more accurate search term.. \nor look up packs here https://cursemeta.dries007.net/search#modpacks");
                                foreach (var p in projects ) {
                                    var authors = String.Join(", ", p.Authors.Select(a => a.Name).ToArray());
                                    Console.WriteLine($"[{ p.Id }] { p.Name } by { authors }");
                                }
                                return Program.Cleanup(-1);
                            }
                        }
                        
                        modpackProject = projects[0];
                        Console.WriteLine($"INFO: Modpack = '{modpackProject.Name}'");                        
                        projectid = modpackProject.Id;
                    }
                    
                    if ( list ) {
                        Console.WriteLine($"Modpack:\n  [{ modpackProject.Id }] { modpackProject.Name }\n  Stage: { modpackProject.Stage }\n  Status: { modpackProject.Status }");
                        Console.WriteLine("Files:");
                        var files = await CurseMeta.Instance.GetAddonFiles(projectid);
                        if(opMCVersion.HasValue())
                            files = files.Where(f => f.GameVersion.Contains(opMCVersion.Value())).ToArray();
                        
                        foreach (var f in files.OrderBy(f => f.FileDate)) {
                            var thing = new {
                                Id = f.Id,
                                MinecraftVersions = f.GameVersion,
                                Date = f.FileDate,
                                Filename = new {
                                    Fancy = f.FileName,
                                    Disk = f.FileNameOnDisk
                                },
                                Status = f.FileStatus,
                                Type = f.ReleaseType,
                                Dependencies = f.Dependencies.Count() != 0 ? f.Dependencies : null
                            };
                            Console.WriteLine(thing.ToPrettyYaml());
                        }
                        return Program.Cleanup();
                    }
                    
                    if (argFile.Value != null) {
                        if (int.TryParse(argFile.Value, out fileid)) {
                            try {
                                modpackfile = await CurseMeta.Instance.GetAddonFile(projectid, fileid);
                            } catch ( AggregateException e ) {
                                Console.WriteLine($"ERROR: { e.Message }");
                                Console.WriteLine($"{ e.InnerException?.Message }");
                                Console.WriteLine("ERROR: Request failed, are you sure the file id is correct?");
                            }
                        } else {
                            if(Release.TryParse(argFile.Value, true, out release)) {
                                Console.WriteLine($"INFO: Filtering for '{ release }' file");
                                hasRelease = true;
                            } else {
                                Console.WriteLine($"INFO: Version filter: '{ argFile.Value }'");
                            }
                        }
                    } else {
                        Console.WriteLine($"INFO: No file or version given, defaulting to '{ Release.Recommended }'");
                        release = Release.Recommended;
                        hasRelease = true;
                    }
                    
                    if (modpackfile == null) {
                        var files = await CurseMeta.Instance.GetAddonFiles(projectid);
                        files = files.OrderBy(f => f.FileDate)
                            .Where(f => f.FileStatus != Alpacka.Lib.Curse.FileStatus.Deleted)
                            .ToArray();
                        if(opMCVersion.HasValue())
                            files = files.Where(f => f.GameVersion.Contains(opMCVersion.Value())).ToArray();
                        
                        ReleaseType releaseType;
                            
                        if(hasRelease) {
                            if (release == Release.Recommended) {
                                modpackfile = files.FirstOrDefault(f => f.ReleaseType == ReleaseType.Release);
                            } else if (release == Release.Latest) {
                                modpackfile = files.FirstOrDefault();
                            }
                        } else if (argFile.Value != null && ReleaseType.TryParse(argFile.Value, true, out releaseType)) {
                            modpackfile = files.FirstOrDefault(f => f.ReleaseType == releaseType);
                        } else {
                            modpackfile = files.FirstOrDefault (
                                file => (
                                    file.FileName.Contains(argFile.Value, StringComparison.OrdinalIgnoreCase) || 
                                    file.FileNameOnDisk.Contains(argFile.Value, StringComparison.OrdinalIgnoreCase)
                                )
                            );
                        }
                        if(modpackfile == null && files.Length > 0) {
                            Console.WriteLine($"WARNING: nothing matched from a total of { files.Length } files");
                            Console.WriteLine("WARNING: try using a different version/file filter e.g: 'latest'");
                        }
                    }
                }
                
                String filepath = null;
                
                if (modpackfile != null) {
                    url = modpackfile.DownloadURL;
                } else if (isUrl) {
                    url = argProject.Value;
                } else if (isFile) {
                    filepath = argProject.Value.CutStart("file:");
                } else {
                    Console.Error.WriteLine("ERROR: Cannot find file/version");
                    return -404;
                }
                    
                var tempExtractPath = instanceHandler.GetInstancePath(Guid.NewGuid().ToString(), Directory.GetCurrentDirectory());
                
                if ( filepath == null) {
                    using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "curseimport")))
                    using (var downloader = new FileDownloader(fileCache))
                    {
                        var file = await downloader.Download(url);
                        Console.WriteLine($"downloaded: { file.FileName }");
                        Console.WriteLine($"location: { file.FullPath }");
                        filepath = file.FullPath;
                    }
                }
                bool success = false;
                string instancePath = null;
                try {
                    // unzip pack
                    ZipFile zf = null;
                    try {
                        FileStream fs = File.OpenRead(filepath);
                        zf = new ZipFile(fs);
                        // if (!String.IsNullOrEmpty(password)) {
                        //     zf.Password = password;     // AES encrypted entries are handled automatically
                        // }
                        foreach (ZipEntry zipEntry in zf) {
                            if (!zipEntry.IsFile) {
                                continue;           // Ignore directories
                            }
                            String entryFileName = zipEntry.Name;
                            // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                            // Optionally match entrynames against a selection list here to skip as desired.
                            // The unpacked length is available in the zipEntry.Size property.

                            byte[] buffer = new byte[4096];     // 4K is optimum
                            Stream zipStream = zf.GetInputStream(zipEntry);

                            // Manipulate the output filename here as desired.
                            String fullZipToPath = Path.Combine(tempExtractPath, entryFileName);
                            string directoryName = Path.GetDirectoryName(fullZipToPath);
                            if (directoryName.Length > 0)
                                Directory.CreateDirectory(directoryName);

                            // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                            // of the file, but does not waste memory.
                            // The "using" will close the stream even if an exception occurs.
                            using (FileStream streamWriter = File.Create(fullZipToPath)) {
                                StreamUtils.Copy(zipStream, streamWriter, buffer);
                            }
                        }
                    } finally {
                        if (zf != null) {
                            zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                            zf.Close(); // Ensure we release resources
                        }
                    }
                    
                    Console.WriteLine($"extracted: { tempExtractPath }");
                    
                    JsonSerializerSettings _settings = new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        MissingMemberHandling = MissingMemberHandling.Ignore 
                    };
                    
                    // parse json
                    var manifest = JsonConvert.DeserializeObject<PackManifest>(File.ReadAllText(Path.Combine(tempExtractPath, "manifest.json")), _settings);
                    
                    //Console.WriteLine(File.ReadAllText(Path.Combine(tempExtractPath, "manifest.json")));
                    if(projectid < 0 && manifest.ProjectID != 0) {
                        projectid = manifest.ProjectID;
                    }
                    if(projectid != manifest.ProjectID) {
                        Console.WriteLine($"old id: { projectid } new id: { manifest.ProjectID }");
                    }
                    
                    try {
                        modpackProject = await CurseMeta.Instance.GetAddon(projectid);
                    } catch (Exception e) {
                        Console.Error.Write("EXCEPTION: " + e.Message);
                    }
                    
                    // Console.WriteLine(manifest.ToPrettyYaml());
                    var Name = manifest.Name ?? modpackProject.Name;
                    var Version = manifest.Version ?? argFile.Value ?? "???";
                    var FullName = Name;
                    if( !String.IsNullOrEmpty(Version) ) FullName += " v" + Version;
                    
                    var safeName = string.Join("_", Name.Split(Path.GetInvalidPathChars()));
                    var safeVersion = string.Join("_", Version.Split(Path.GetInvalidPathChars()));
                    var safeFullName = string.Join("_", FullName.Split(Path.GetInvalidPathChars()));
                    instancePath = instanceHandler.GetInstancePath(safeFullName, Directory.GetCurrentDirectory());
                    var configPathFile = Path.Combine(instancePath, Constants.PACK_CONFIG_FILE);
                    
                    if (File.Exists(configPathFile)) {
                        // TODO: We really need that logging stuffs.
                        // Console.WriteLine($"ERROR: { Constants.PACK_CONFIG_FILE } already exists in the target directory.");
                        throw new EntryExistsException($"{ Constants.PACK_CONFIG_FILE } already exists in the target directory.");
                    }
                    
                    // general info
                    Console.WriteLine($"id: { projectid }");
                    var description = $"{ Name } { Version } by { manifest.Author ?? "unknown" } projectID: { projectid }";
                    
                    //TODO: process files, get project names and file names (versions)
                    
                    var modpack = new ModpackConfig();
                    
                    modpack.MinecraftVersion = manifest.Minecraft.Version;
                    
                    var mcVersion = new System.Version(modpack.MinecraftVersion);
                    modpack.Includes = new EntryIncludes{ ForceMappingStyle = true };
                    var modGroup = new EntryIncludes.Group("mods");
                    var latestGroup = new EntryIncludes.Group("latest");
                    modGroup.Add(latestGroup);
                    modpack.Includes.Add(modGroup);
                    
                    async Task<EntryResource> processFile(PackFile file) 
                    {
                        var addon = await CurseMeta.Instance.GetAddon(file.ProjectID);
                        Console.WriteLine($"mod: { file.ProjectID } file: { file.FileID }");
                        var addonFile = await CurseMeta.Instance.GetAddonFile(file.ProjectID, file.FileID);
                        var name = addon.Name.Trim();
                        var version = addonFile.FileName.Trim().TrimEnd(".jar".ToCharArray());
                        
                        // test version 
                        var testFileID = await ResourceHandlerCurse.FindFileId(
                            addon, 
                            new EntryMod
                            {
                                Name = name,
                                Version = version,
                            },
                            modpack.MinecraftVersion,
                            true
                        );
                        
                        var resource = new EntryMod {
                            Source = name,
                            Version = version,
                            Path = null,
                            Description = $"projectID: { addon.Id }\nFileID: { addonFile.Id }",
                            Links = new EntryLinks {
                                Website = addon.WebSiteURL
                            }
                        };
                        
                       if(testFileID != addonFile.Id) {
                            Console.WriteLine($"{ testFileID } != { addonFile.Id }");
                            resource.Version = $"$:{addonFile.Id}";
                        }
                        return resource;
                    }
                    
                    latestGroup.AddRange(await Task.WhenAll(manifest.Files.Select(processFile)));
                    
                    modpack.Name = FullName;
                    if(modpackProject != null) {
                        modpack.Authors = modpackProject.Authors.Select(a => a.Name).ToList();
                    } else {
                        modpack.Authors = new List<string>();
                        modpack.Authors.Add(manifest.Author);
                    }
                    modpack.Authors.Add(Environment.GetEnvironmentVariable("USERNAME") ?? "...");

                    var primaryModloader = manifest.Minecraft.ModLoaders.FirstOrDefault(m => m.primary == true);
                    if (primaryModloader != null)
                        modpack.ForgeVersion = primaryModloader.Id.CutStart("forge-");
                    
                    // Console.WriteLine($"generated config: \n{ defaultConfig }");
                    
                    Directory.CreateDirectory(instancePath);
                    
                    //ModpackConfig.SaveYAML(instancePath, modpack);
                    
                    modpack.SaveYAML(instancePath);
                    
                    using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "curseicon")))
                    using (var downloader = new FileDownloader(fileCache))
                    {
                        var icon = modpackProject.Attachments.FirstOrDefault(a => a.IsDefault);
                        if(icon != null) {
                            var file = await downloader.Download(icon.ThumbnailUrl);
                            Console.WriteLine($"downloaded: { file.FileName }");
                            Console.WriteLine($"location: { file.FullPath }");
                            File.Copy(file.FullPath, Path.Combine(instancePath, "icon.png"));
                        }
                    }
                    
                    var build = ModpackBuild.CopyFrom(modpack);
                    /*ModpackBuild build = null;
                    try {
                        build = await CommandBuild.Build(modpack);
                    } catch (Exception e) {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                    }*/
                    
                    instanceHandler.Install(instancePath, build);
                    
                    var info = new AlpackaInfo { InstanceType = instanceHandler.Name };
                    info.Save(instancePath);
                    
                    var overrides = Path.Combine(tempExtractPath, manifest.Overrides);
                    Console.WriteLine($"overrides: { overrides }");
                    if(Directory.Exists(overrides))
                        Directory.Move(overrides, Path.Combine(instancePath, Constants.MC_DIR));
                    
                    Console.WriteLine($"Created stub alpacka pack in { Path.GetFullPath(instancePath) }");
                    Console.WriteLine($"Edit { Constants.PACK_CONFIG_FILE } and run 'alpacka update'");
                    
                    var modPath = Path.Combine(instancePath, Constants.MC_MODS_DIR);
                    var modsCount = Directory.Exists(modPath) ? Directory.GetFiles(modPath).Length : -1;
                    if(modsCount > 0) {
                        Console.WriteLine($"WARNING: This modpack bundles { modsCount } mods");
                        Console.WriteLine($"WARNING:THIS IS NOT SUPPORTED AND THESE FILES WILL BE OVERRIDDEN, so please find the sources or rehost them");
                    }
                    var coremodsPath = Path.Combine(instancePath, "coremods");
                    var coremodsCount = Directory.Exists(coremodsPath) ? Directory.GetFiles(coremodsPath).Length : -1;
                    if(coremodsCount > 0) {
                        Console.WriteLine($"WARNING: This modpack bundles { modsCount } coremods");
                        Console.WriteLine($"WARNING:THIS IS NOT SUPPORTED, COREMODS ARE NOT MANAGED BY ALPACKA, so for now.. sorry you cannot use this pack");
                    }
                    
                    if(Directory.Exists(Path.Combine(instancePath, "coremods"))) {
                        Console.WriteLine("WARNING: bundles libraries will not work");
                    }
                    
                    var jarmodsPath = Path.Combine(instancePath, "jarmods");
                    var jarmodsCount = Directory.Exists(modPath) ? Directory.GetFiles(jarmodsPath).Length : -1;
                    if(jarmodsCount > 0) {
                        Console.WriteLine($"WARNING: This modpack bundles { modsCount } jarmods");
                        Console.WriteLine($"WARNING:THIS IS NOT SUPPORTED, JAR MODS CANNOT BE INSTALLED BY ALPACKA for now.. sorry you cannot use this pack");
                    }
                    //git init, TODO: gitignore etc..
                    
                    Repository.Init(instancePath);
                    
                    Console.WriteLine($"Initialized git repo in { Path.GetFullPath(instancePath) }");
                    Console.WriteLine($"Creating Initial commit, This could take a second...");
                    using (var repo = new Repository(instancePath))
                    {
                        LibGit2Sharp.Commands.Stage(repo, "*");
                        
                        var config = repo.Config;
                        
                        var name = config.GetValueOrDefault<string>("user.name", "nobody");
                        var email = config.GetValueOrDefault<string>("user.email", "@example.com");
                        Signature user = new Signature(name, email, DateTime.Now);
                        
                        // Commit to the repository
                        var message = $"Initial Commit\nPack generated from { url }";
                        Commit commit = repo.Commit(message, user, user);
                        
                        Console.WriteLine($"Commit Message: \n{ message }");
                        
                        Console.WriteLine($"Created Inital Commit as");
                        Console.WriteLine($"\tuser.name: { name }");
                        Console.WriteLine($"\tuser.name: { email }");
                    }
                    
                    Console.WriteLine($"Created stub alpacka pack in { Path.GetFullPath(instancePath) }");
                    Console.WriteLine($"Edit { Constants.PACK_CONFIG_FILE } and run 'alpacka build' && 'alpacka update'");
                    success = true;
                } catch (EntryExistsException e) {
                    Console.Error.WriteLine("EXCEPTION: " + e.Message);
                } catch (Exception e) {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.InnerException?.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    Console.Error.WriteLine(e.InnerException?.StackTrace);
                    /*if(instancePath != null && Directory.Exists(instancePath)) {
                        Console.WriteLine($"deleting files in { instancePath }");
                        Directory.Delete(instancePath, true);
                    }*/
                } finally {
                    if(Directory.Exists(tempExtractPath)) {
                        Console.WriteLine($"deleting extracted files in { tempExtractPath }");
                        Directory.Delete(tempExtractPath, true);
                    }
                }
                if(success)
                    Console.WriteLine("Modpack Imported Sucessfully");
                return Program.Cleanup();
            });
        }
    }
}
