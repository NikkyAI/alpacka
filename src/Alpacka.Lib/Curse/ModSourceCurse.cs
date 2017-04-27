using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Alpacka.Lib.Mods;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Curse
{
    public class ModSourceCurse : IModSource
    {
        // TODO: Don't use concurrent dictionary - in general this could likely be done better.
        private static readonly ConcurrentDictionary<EntryMod, AddonFile> _modToAddonFile =
            new ConcurrentDictionary<EntryMod, AddonFile>();
        private static readonly ConcurrentDictionary<EntryMod, DependencyType> _modToDependencyType =
            new ConcurrentDictionary<EntryMod, DependencyType>();
            
        private ProjectList _allProjects;
        
        public bool CanHandle(string source) =>
            (source.StartsWith("curse:") || !source.Contains(":"));
        
        public async Task Initialize()
        {
            _allProjects = await ProjectFeed.Get();
        }
        
        public async Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency)
        {
            var id = -1;
            var splitSource = mod.Source.Split(new char[]{ ':' }, 2);
            var source = splitSource[splitSource.Length-1].Trim();
            var scheme = (splitSource.Length > 1) ? splitSource[0] : "";
            
            DependencyType type;
            if (!_modToDependencyType.TryGetValue(mod, out type))
                type = DependencyType.Required;
            var optional = (type == DependencyType.Optional);
            
            Addon addon;
            if (!int.TryParse(source, out id)) {
                addon = _allProjects.Data.Find(a => string.Equals(a.Name.Trim(), source, StringComparison.OrdinalIgnoreCase));
                if (addon == null)
                    throw new Exception($"No Project of name '{ source }' found");
                // Debug.WriteLine(_Addon.ToPrettyJson());
                id = addon.Id;
                Debug.WriteLine($"get full Addon info for { addon.Name }");
            } else Debug.WriteLine($"get full Addon info for id: { source }");
            
            addon = await CurseMeta.GetAddon(id);
            mod.Name        = mod.Name ?? addon.Name;
            mod.Description = mod.Description ?? addon.Summary;
            if (mod.Links == null) mod.Links = new EntryLinks();
            mod.Links.Website   = mod.Links.Website ?? addon.WebSiteURL;
            mod.Links.Source    = mod.Links.Source ?? addon.ExternalUrl;
            mod.Links.Donations = mod.Links.Donations ?? addon.DonationUrl;
            
            var fileId = await FindFileId(addon, mod, mcVersion, optional);
            if (fileId == -1) {
                if (optional) {
                    Debug.WriteLine($"no file found for { mod.Source } This is not a Error");
                    return null; // We do not throw a error because its not required
                // We should probably not reach this point ever:
                } else throw new Exception($"No File of type 'Release' found for { mod.Name } in { mcVersion }");
            }
            
            var fileInfo = await CurseMeta.GetAddonFile(addon.Id, fileId);
            _modToAddonFile[mod] = fileInfo;
            foreach (var dep in fileInfo.Dependencies) {
                if (dep.Type == DependencyType.Required) {
                    var depAddon = await CurseMeta.GetAddon(dep.AddonId);
                    var depMod = new EntryMod {
                        Source = $"curse:{ dep.AddonId }",
                        Name = depAddon.Name,
                        Side = mod.Side,
                        Version = Release.Latest.ToString() // avoid crashes from listing files
                    };
                    _modToDependencyType[depMod] = dep.Type;
                    addDependency(depMod);
                } else if (dep.Type == DependencyType.Optional) {
                    var depAddon = await CurseMeta.GetAddon(dep.AddonId);
                    // TODO: Make this available in some form in the return value.
                    Console.WriteLine($"'{ mod.Name }' recommends using '{ depAddon.Name }'");
                }
            }
            return fileInfo.DownloadURL;
        }
        
        public async Task<int> FindFileId(Addon addon, EntryMod mod, string mcVersion, bool optional)
        {
            // Debug.WriteLine($"find file\n mcVersion: { mcVersion }\n name: { Addon.Name }"); // TODO: verbose logging
            // Debug.WriteLine($"Addon: { Addon.ToPrettyJson() }"); // TODO: verbose logging
            if (string.Equals(mod.Version, Release.Recommended.ToString(), StringComparison.OrdinalIgnoreCase)) {
                
                var addonFiles = await CurseMeta.GetAddonFiles(addon.Id);
                
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
                
                var addonFiles = await CurseMeta.GetAddonFiles(addon.Id);
                var sorted = addonFiles.OrderBy(f => f.FileDate).ToList();
                
                Debug.WriteLine($"mod.Name: { mod.Name } mcVersion: { mcVersion } mod.Version: { mod.Version }");
                var latestFile = sorted.Find(file => (file.GameVersion.Contains(mcVersion) &&
                                                      file.FileName.Contains(mod.Version)));
                if (latestFile != null) return latestFile.Id;
                
            }
            return -1;
        }
    }
}
