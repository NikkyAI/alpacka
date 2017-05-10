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
            var argZip = Argument("zip",
                "url of the curse modpack zipfile");
            
            //TODO: allow using search function for getting project an file id
            
            HelpOption("-? | -h | --help");
            
            OnExecute(async () => {
                var instanceHandler = AlpackaRegistry.InstanceHandlers[argType.Value];
                if (instanceHandler == null) {
                    Console.WriteLine($"ERROR: No handler for type '{ argType.Value }'");
                    return 1;
                }
                
                // var tempDirName   = Guid.NewGuid().ToString();
                var tempExtractPath = instanceHandler.GetInstancePath(Guid.NewGuid().ToString(), Directory.GetCurrentDirectory());
                    
                using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "curseimport")))
                using (var downloader = new FileDownloader(fileCache))
                {
                    var file = await downloader.Download(argZip.Value);
                    Console.WriteLine($"downloaded: { file.FileName }");
                    Console.WriteLine($"location: { file.FullPath }");
                    
                    // unzip pack
                    
                    ZipFile zf = null;
                    try {
                        FileStream fs = File.OpenRead(file.FullPath);
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
                }
                
                JsonSerializerSettings _settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    MissingMemberHandling = MissingMemberHandling.Ignore 
                };
                
                //TODO: parse json
                var manifest = JsonConvert.DeserializeObject<PackManifest>(File.ReadAllText(Path.Combine(tempExtractPath, "manifest.json")), _settings);
                
                var safeName = string.Join("_", manifest.Name.Split(Path.GetInvalidPathChars()));
                var safeVersion = string.Join("_", manifest.Version.Split(Path.GetInvalidPathChars()));
                var instancePath = instanceHandler.GetInstancePath(safeName + "-" + safeVersion, Directory.GetCurrentDirectory());
                var configPathFile = Path.Combine(instancePath, Constants.PACK_CONFIG_FILE);
                
                if (File.Exists(configPathFile)) {
                    // TODO: We really need that logging stuffs.
                    Console.WriteLine($"ERROR: { Constants.PACK_CONFIG_FILE } already exists in the target directory.");
                    Console.WriteLine($"deleting extracted files in { tempExtractPath }");
                    Directory.Delete(tempExtractPath, true);
                    return 1;
                }
                
                // general info
                Console.WriteLine($"id: { manifest.ProjectID}");
                var description = $"{ manifest.Name } { manifest.Version } by { manifest.Author } projectID: { manifest.ProjectID }";
                
                //TODO: process files, get project names and file names (versions)
                
                
                var modpack = new ModpackConfig();
                
                
                modpack.Includes = new EntryIncludes();
                var modGroup = new EntryIncludes.Group("mods");
                var latestGroup = new EntryIncludes.Group("latest");
                modGroup.Add(latestGroup);
                modpack.Includes.Add(modGroup);
                
                async Task<EntryResource> processFile(PackFile file) 
                {
                    Console.WriteLine($"mod: { file.ProjectID } file: { file.FileID }");
                    var addon = await CurseMeta.GetAddon(file.ProjectID);
                    var addonFile = await CurseMeta.GetAddonFile(file.ProjectID, file.FileID);
                    var name = addon.Name.Trim();
                    var version = addonFile.FileName.Trim().TrimEnd(".jar".ToCharArray());
                    Console.WriteLine($"mod: { file.ProjectID } file: { file.FileID } version: { version }");
                    var resoure = new EntryResource {
                        //Name = name,
                        Source = name,
                        Version = version
                    };
                    return resoure;
                }
                
                foreach (var f in manifest.Files) {
                    var entry = await processFile(f);
                    latestGroup.Add(entry);
                    await Task.Delay(TimeSpan.FromSeconds(.1));
                }
                
                modpack.Name = manifest.Name;
                modpack.Authors = new List<string>();
                modpack.Authors.Add(manifest.Author);
                modpack.Authors.Add(Environment.GetEnvironmentVariable("USERNAME") ?? "...");

                modpack.MinecraftVersion = manifest.Minecraft.Version;
                modpack.ForgeVersion = manifest.Minecraft.ModLoaders[0].Id.TrimStart("forge-".ToCharArray());
                
                // Console.WriteLine($"generated config: \n{ defaultConfig }");
                
                Directory.CreateDirectory(instancePath);
                
                var serializer = new Serializer();
                File.WriteAllText(configPathFile, serializer.Serialize(modpack));
                
                var build = await CommandBuild.Build(modpack);
                
                //TODO: build modpack and call instanceHandler Install
                instanceHandler.Install(instancePath, build);
                
                var info = new AlpackaInfo { InstanceType = instanceHandler.Name };
                info.Save(instancePath);
                
                var overrides = Path.Combine(tempExtractPath, manifest.Overrides);
                Console.WriteLine($"overrides: { overrides }");
                Directory.Move(overrides, Path.Combine(instancePath, Constants.MC_DIR));
                
                Directory.Delete(tempExtractPath, true);
                
                //TODO: git init, gitignore etc..
                
                Repository.Init(instancePath);
                
                using (var repo = new Repository(instancePath))
                {
                    LibGit2Sharp.Commands.Stage(repo, "*");
                    
                    var config = repo.Config;
                    
                    var name = config.GetValueOrDefault<string>("user.name", "nobody");
                    var email = config.GetValueOrDefault<string>("user.email", "@example.com");
                    Signature user = new Signature(name, email, DateTime.Now);
                    
                    // Commit to the repository
                    Commit commit = repo.Commit($"Initial Commit\nPack generated from { argZip.Value }", user, user);
                }
                
                Console.WriteLine($"Created stub alpacka pack in { Path.GetFullPath(instancePath) }");
                Console.WriteLine($"Edit { Constants.PACK_CONFIG_FILE } and run 'alpacka update'");
                
                return 0;
            });
        }
    }
}
