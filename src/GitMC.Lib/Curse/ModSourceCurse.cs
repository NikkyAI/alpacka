using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitMC.Lib.Config;
using GitMC.Lib.Mods;

namespace GitMC.Lib.Curse
{
    public class ModSourceCurse : IModSource
    {
        private static readonly ConcurrentDictionary<EntryMod, AddonFile> _modToAddonFile =
            new ConcurrentDictionary<EntryMod, AddonFile>();
        private static readonly ConcurrentDictionary<EntryMod, DependencyType> _modToDependencyType =
            new ConcurrentDictionary<EntryMod, DependencyType>();
            
        private Lazy<Task<ProjectList>> AllProjects { get; } = new Lazy<Task<ProjectList>>(LatestProjects.Get);
        
        public bool CanHandle(string source) =>
            (source.StartsWith("curse:") || !source.Contains(":"));
        
        public async Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency) {
            var allProjects = await AllProjects.Value;
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
                addon = allProjects.Data.Find(a => string.Equals(a.Name.Trim(), source, StringComparison.OrdinalIgnoreCase));
                if (addon == null)
                    throw new DownloaderException($"No Project of name '{ source }' found");
                // Console.WriteLine(_addon.ToPrettyJson());
                id = addon.Id;
                Console.WriteLine($"get full addon info for { addon.Name }");
            } else Console.WriteLine($"get full addon info for id: { source }");
            
            addon = await CurseProxy.GetAddon(id);
            mod.Name        = mod.Name ?? addon.Name;
            mod.Description = mod.Description ?? addon.Summary;
            if (mod.Links == null) mod.Links = new EntryLinks();
            mod.Links.Website   = mod.Links.Website ?? addon.WebSiteURL;
            mod.Links.Source    = mod.Links.Source ?? addon.ExternalUrl;
            mod.Links.Donations = mod.Links.Donations ?? addon.DonationUrl;
            
            var fileId = await FindFileId(addon, mod, mcVersion, optional);
            if (fileId == -1) {
                if (optional) {
                    Console.WriteLine($"no file found for { mod.Source.ToPrettyJson() } This is not a Error");
                    return null; // We do not throw a error because its not required
                // We should probably not reach this point ever:
                } else throw new DownloaderException($"No File of type 'Release' found for { mod.Name } in { mcVersion }");
            }
            
            var fileInfo = await CurseProxy.GetAddonFile(addon.Id, fileId);
            _modToAddonFile[mod] = fileInfo;
            foreach (var dep in fileInfo.Dependencies) {
                if (dep.Type == DependencyType.Required) {
                    var depAddon = await CurseProxy.GetAddon(dep.AddOnId);
                    var depMod = new EntryMod {
                        Source = $"curse:{ dep.AddOnId }",
                        Name = depAddon.Name,
                        Side = mod.Side,
                        Version = Release.Latest.ToString() // avoid crashes from listing files
                    };
                    _modToDependencyType[depMod] = dep.Type;
                    addDependency(depMod);
                } else if (dep.Type == DependencyType.Optional) {
                    var depAddon = await CurseProxy.GetAddon(dep.AddOnId);
                    Console.WriteLine($"'{ mod.Name }' recommends using '{ depAddon.Name }'"); // TODO: queue and print all at once
                }
            }
            return fileInfo.DownloadURL;
        }
        
        public async Task<int> FindFileId(Addon addon, EntryMod mod, string mcVersion, bool optional)
        {
            // Console.WriteLine($"find file\n mcVersion: { mcVersion }\n name: { addon.Name }"); // TODO: verbose logging
            // Console.WriteLine($"addon: { addon.ToPrettyJson() }"); // TODO: verbose logging
            if (string.Equals(mod.Version, Release.Recommended.ToString(), StringComparison.OrdinalIgnoreCase)) {
                
                try {
                    var addonFiles = await CurseProxy.GetAddonFiles(addon.Id); // FIXME: This can crash with error 500 for OpenComputers, JEI, RFTools etc
                    
                    var sorted = new List<AddonFile>(addonFiles.Files);
                    sorted.Sort((f1, f2) => f1.FileDate.CompareTo(f2.FileDate) );
                    var recommendedFile = sorted.Find(file => (file.GameVersion.Contains(mcVersion) &&
                                                               (file.ReleaseType == ReleaseType.Release)));
                    if (recommendedFile == null) {
                        if (optional) return -1;
                        else throw new DownloaderException($"No File of type 'Release' found for { mod.Name } in { mcVersion }");
                    }
                    
                    return recommendedFile.Id;
                } catch(Exception e) { Console.WriteLine($"failed for { addon.Name } with { e.Message }"); }
                
            } else if (string.Equals(mod.Version, Release.Latest.ToString(), StringComparison.OrdinalIgnoreCase)) {
                
                var latestFile = addon.GameVersionLatestFiles.Find(file => (file.GameVesion == mcVersion));
                if (latestFile != null) return latestFile.ProjectFileId;
                
            } else {
                
                var addonFiles = await CurseProxy.GetAddonFiles(addon.Id);
                var sorted = addonFiles.Files;
                sorted.Sort((f1, f2) => f1.FileDate.CompareTo(f2.FileDate));
                
                Console.WriteLine($"mod.Name: { mod.Name } mcVersion: { mcVersion } mod.Version: { mod.Version }");
                var latestFile = sorted.Find(file => (file.GameVersion.Contains(mcVersion) &&
                                                      file.FileName.Contains(mod.Version)));
                if (latestFile != null) return latestFile.Id;
                
            }
            return -1;
        }
    }
}
