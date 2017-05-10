using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;
using Alpacka.Lib;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Curse;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
                
                var tempDirName   = Guid.NewGuid().ToString();
                var tempExtractPath = instanceHandler.GetInstancePath(tempDirName, Directory.GetCurrentDirectory());
                    
                using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "curseimport")))
                using (var downloader = new FileDownloaderURL(fileCache))
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
                
                var instancePath = instanceHandler.GetInstancePath(safeName, Directory.GetCurrentDirectory());
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
                
                // var description = await CurseMeta.GetAddonDescription(manifest.ProjectID);
                // Console.WriteLine($"description: { description }");
                
                // TODO: Move this to a utility method. (In Alpacka.Lib?)
                var resourceStream = GetType().GetTypeInfo().Assembly
                    .GetManifestResourceStream("Alpacka.CLI.Resources.packconfig.default.yaml");
                string defaultConfig;
                using (var reader = new StreamReader(resourceStream))
                    defaultConfig = reader.ReadToEnd();
                
                var packName = "...";
                var authors = Environment.GetEnvironmentVariable("USERNAME") ?? "...";
                
                //TODO: get forge version from modloader entry in manifest > minecraft > modLoaders
                var forgeData    = ForgeVersionData.Download().Result;
                var mcVersion    = forgeData.GetRecentMCVersion(Release.Recommended);
                var forgeVersion = forgeData.GetRecent(mcVersion, Release.Recommended)?.GetFullVersion();
                
                defaultConfig = Regex.Replace(defaultConfig, "{{(.+)}}", match => {
                    switch (match.Groups[1].Value.Trim()) {
                        case "NAME": return packName;
                        case "AUTHORS": return authors;
                        case "MC_VERSION": return mcVersion;
                        case "FORGE_VERSION": return forgeVersion;
                        default: return "...";
                    }
                });
                
                // Directory.CreateDirectory(instancePath);
                
                var info = new AlpackaInfo { InstanceType = instanceHandler.Name };
                info.Save(instancePath);
                // File.WriteAllText(configPathFile, defaultConfig);
                
                Console.WriteLine($"Created stub alpacka pack in { Path.GetFullPath(instancePath) }");
                Console.WriteLine($"Edit { Constants.PACK_CONFIG_FILE } and run 'alpacka update'");
                
                return 0;
            });
        }
    }
}
