using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Resources;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Curse
{
    public class ResourceHandlerCurse : IResourceHandler
    {
        // TODO: Don't use concurrent dictionary - in general this could likely be done better.
        private static readonly ConcurrentDictionary<EntryMod, DependencyType> _modToDependencyType =
            new ConcurrentDictionary<EntryMod, DependencyType>();
            
        private ProjectList _allProjects;

        public string Name => "Curse";
        
        public async Task Initialize() => _allProjects = await ProjectFeed.GetMods();
        
        public bool ShouldOverwriteHandler(string source) => false;
        
        public async Task<EntryResource> Resolve(EntryResource resource, string mcVersion,
                                                 Action<EntryResource> addDependency)
        {
            var mod = EntryMod.Convert(resource);
            
            DependencyType type;
            if (!_modToDependencyType.TryGetValue(mod, out type))
                type = DependencyType.Required;
            var optional = (type == DependencyType.Optional);
            
            var id = -1;
            // If Source contains an ID, use it.
            if (int.TryParse(mod.Source, out id))
                Debug.WriteLine($"get full Addon info for id: { mod.Source }");
            // Otherwise try to find an addon with Source as its name.
            else {
                // Use AllProjects data to find the addon, which doesn't contain all
                // the necessary information so only the ID is interesting to us.
                var addonForId = _allProjects.Data.Find(a => a.Name.Trim().Equals(mod.Source, StringComparison.OrdinalIgnoreCase));
                // TODO: Finding in a large list is pretty slow. Use a Dictionary?
                if (addonForId == null) throw new Exception(
                    $"No Project of name '{ mod.Source }' found");
                // Debug.WriteLine(_Addon.ToPrettyJson());
                id = addonForId.Id;
                Debug.WriteLine($"get full Addon info for { addonForId.Name }");
            }
            
            var addon = await CurseMeta.Instance.GetAddon(id);
                
            mod.Name        = mod.Name ?? addon.Name;
            mod.Description = mod.Description ?? addon.Summary;
            if (mod.Links == null) mod.Links = new EntryLinks();
            mod.Links.Website   = mod.Links.Website ?? addon.WebSiteURL;
            mod.Links.Source    = mod.Links.Source ?? addon.ExternalUrl;
            mod.Links.Donations = mod.Links.Donations ?? addon.DonationUrl;
            
            int fileId = -1;
            if(!(mod.Version.StartsWith("$:") && int.TryParse(mod.Version.CutStart("$:"), out fileId)))
            {
                fileId = await FindFileId(addon, mod, mcVersion, optional);
                if (fileId == -1) {
                    if (optional) {
                        Debug.WriteLine($"no file found for { mod.Source } This is not a Error");
                        return null; // We do not throw a error because its not required
                    // We should probably not reach this point ever:
                    } else throw new Exception($"No File of type 'Release' found for { mod.Name } in { mcVersion }");
                }
            }
            var fileInfo = await CurseMeta.Instance.GetAddonFile(addon.Id, fileId);
            mod.Source = fileInfo.DownloadURL;
            mod.Path  = Path.Combine(mod.Path, fileInfo.FileNameOnDisk);
            
            foreach (var dep in fileInfo.Dependencies) {
                if (dep.Type == DependencyType.Required) {
                    var depAddon = await CurseMeta.Instance.GetAddon(dep.AddonId);
                    var depMod = new EntryMod {
                        Name    = depAddon.Name,
                        Handler = Name,
                        Source  = dep.AddonId.ToString(),
                        Version = Release.Recommended.ToString(), //TODO: apply same defaults to mod entry on callback
                        Side    = mod.Side,
                        Path = "mods"
                    };
                    _modToDependencyType[depMod] = dep.Type;
                    addDependency(depMod);
                } else if (dep.Type == DependencyType.Optional) {
                    var depAddon = await CurseMeta.Instance.GetAddon(dep.AddonId);
                    // TODO: Make this available in some form in the return value.
                    Console.WriteLine($"'{ mod.Name }' recommends using '{ depAddon.Name }'");
                }
            }
            
            return mod;
            
        }
        
        public static async Task<int> FindFileId(Addon addon, EntryMod mod, string mcVersion, bool optional)
        {
            // Debug.WriteLine($"find file\n mcVersion: { mcVersion }\n name: { Addon.Name }"); // TODO: verbose logging
            // Debug.WriteLine($"Addon: { addon.ToPrettyJson() }"); // TODO: verbose logging
            
            if (string.Equals(mod.Version, Release.Recommended.ToString(), StringComparison.OrdinalIgnoreCase)) {
                
                var addonFiles = await CurseMeta.Instance.GetAddonFiles(addon.Id);
                
                var sorted = addonFiles.OrderBy(f => f.FileDate).ToList();
                var recommendedFile = sorted.Find(file => (file.GameVersion.Contains(mcVersion) &&
                                                           (file.ReleaseType == ReleaseType.Release)));
                if (recommendedFile == null) {
                    if (optional) return -1;
                    else throw new Exception($"No File of type 'Release' found for { mod.Name } in { mcVersion }");
                }
                
                return recommendedFile.Id;
                
            } else if (string.Equals(mod.Version, Release.Latest.ToString(), StringComparison.OrdinalIgnoreCase)) {
                
                var latestFile = addon.GameVersionLatestFiles.FirstOrDefault(file => (file.GameVesion == mcVersion));
                if (latestFile != null) return latestFile.ProjectFileId;
                
            } else {
                
                var addonFiles = await CurseMeta.Instance.GetAddonFiles(addon.Id);
                var sorted = addonFiles.OrderBy(f => f.FileDate).ToList();
                
                Debug.WriteLine($"mod.Name: { mod.Name } mcVersion: { mcVersion } mod.Version: { mod.Version }");
                var latestFile = sorted.Find(file => (file.GameVersion.Contains(mcVersion) &&
                    (file.FileName.Contains(mod.Version) || file.FileNameOnDisk.Contains(mod.Version)))
                );
                if (latestFile != null) return latestFile.Id;
                
            }
            return -1;
        }
    }
}
