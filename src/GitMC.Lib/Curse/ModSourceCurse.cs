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
        private Lazy<Task<ProjectList>> AllProjects = new Lazy<Task<ProjectList>>(LatestProjects.Get);
        private static ConcurrentDictionary<EntryMod, AddonFile> modToAddonFile = new ConcurrentDictionary<EntryMod, AddonFile>();
        private static ConcurrentDictionary<EntryMod, DependencyType> modToDependencyType = new ConcurrentDictionary<EntryMod, DependencyType>();
        
        public bool CanHandle(string source) =>
            (source.StartsWith("curse:") || source.StartsWith("curse_id:") || !source.Contains(":"));
        
        public async Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency) {
            var allProjects = await AllProjects.Value;
            int id = -1;
            var splitSource = mod.Source.Split(new char[]{':'}, 2);
            var source = splitSource[splitSource.Length-1].Trim();
            var scheme = splitSource.Length > 1 ? splitSource[0] : "";
            
            
            DependencyType type;
            if(!modToDependencyType.TryGetValue(mod, out type))
                type = DependencyType.Required;
            var optional = (type == DependencyType.Optional);
            
            Addon addon;
            
            if(int.TryParse(source, out id))
            {
                Console.WriteLine($"get full addon info for id: {source}");
            }
            else
            {
                addon = allProjects.data.Find(__addon => string.Equals(__addon.Name.Trim(), source, StringComparison.OrdinalIgnoreCase));
                if(addon == null)
                    throw new DownloaderException($"No Project of name '{source}' found");
                // Console.WriteLine(_addon.ToPrettyJson());
                
                id = addon.Id;
                Console.WriteLine("get full addon info for " + addon.Name);
            }
            
            addon = await CurseProxy.GetAddon(id);
            mod.Name        = mod.Name ?? addon.Name;
            mod.Description = mod.Description ?? addon.Summary;
            if (mod.Links == null) mod.Links = new EntryLinks();
            mod.Links.Website   = mod.Links.Website ?? addon.WebSiteURL;
            mod.Links.Source    = mod.Links.Source ?? addon.ExternalUrl;
            mod.Links.Donations = mod.Links.Donations ?? addon.DonationUrl;
            
            var fileId = await FindFileId(addon, mod, mcVersion, optional);
            if(fileId == -1) 
            {
                if(optional)
                {
                    Console.WriteLine($"no file found for {mod.Source.ToPrettyJson()} This is not a Error");
                    return null; //we do not throw a error because its not required
                }
                else 
                {
                    //we should probably not reach this point ever
                    throw new DownloaderException($"No File of type 'Release' found for {mod.Name} in {mcVersion}");
                }
                
            }
            var fileInfo = await CurseProxy.GetAddonFile(addon.Id, fileId);
            modToAddonFile[mod] = fileInfo;
            foreach(var dep in fileInfo.Dependencies) {
                if(dep.Type == DependencyType.Required)
                {
                    var depAddon = await CurseProxy.GetAddon(dep.AddOnId);
                    var depMod = new EntryMod {
                        Source = $"curse_id:{dep.AddOnId}",
                        Name = depAddon.Name,
                        Side = mod.Side,
                        Version = DefaultVersion.Latest.ToString() //avoid crashes from listing files
                    };
                    modToDependencyType[depMod] = dep.Type;
                    addDependency(depMod);
                } else if(dep.Type == DependencyType.Optional) {
                    var depAddon = await CurseProxy.GetAddon(dep.AddOnId);
                    Console.WriteLine($"'{mod.Name}' recommends using '{depAddon.Name}'"); //TODO: queue and print all at once
                }
            }
            return fileInfo.DownloadURL;
        }
        
        public async Task<int> FindFileId(Addon addon, EntryMod mod, string mcVersion, bool optional)
        {
            // Console.WriteLine($"find file\n mcVersion: {mcVersion}\n name: {addon.Name}"); //TODO: verbose logging
            // Console.WriteLine($"addon: {addon.ToPrettyJson()}"); //TODO: verbose logging
            if(string.Equals(mod.Version, DefaultVersion.Recommended.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                try {
                    var addonFiles = await CurseProxy.GetAddonFiles(addon.Id); //FIXME: THIS CAN CRASH WITH 500 for OpenCOmputers, JEI, RFTools etc
                    
                    var sorted = new List<AddonFile>(addonFiles.Files);
                    sorted.Sort( (f1,f2) => f1.FileDate.CompareTo(f2.FileDate) );
                    AddonFile recommendedFile = sorted.Find(file => file.GameVersion.Contains(mcVersion) && file.ReleaseType == ReleaseType.Release);
                    if(recommendedFile == null)
                    {
                        // Console.WriteLine($"No File of type 'Release' found for {mod.Name} in {mcVersion}");
                        if(!optional) 
                            throw new DownloaderException($"No File of type 'Release' found for {mod.Name} in {mcVersion}");
                        else
                            return -1;
                    }
                    return recommendedFile.Id;
                } catch(Exception e) {
                    Console.WriteLine($"failed for {addon.Name} with {e.Message}");
                }
            }
            else if(string.Equals(mod.Version, DefaultVersion.Latest.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var latestFile = addon.GameVersionLatestFiles.Find(file => file.GameVesion == mcVersion);
                if(latestFile != null)
                    return latestFile.ProjectFileId;
            }
            else
            {
                var addonFiles = await CurseProxy.GetAddonFiles(addon.Id);
                var sorted = addonFiles.Files;
                sorted.Sort( (f1,f2) => f1.FileDate.CompareTo(f2.FileDate) );
                
                Console.WriteLine($"mod.Name: {mod.Name} mcVersion: {mcVersion} mod.Version: {mod.Version}");
                AddonFile latestFile = sorted.Find(file => file.GameVersion.Contains(mcVersion) && file.FileName.Contains(mod.Version));
                if(latestFile != null)
                    return latestFile.Id;
            }
            return -1;  
        }
    }
}
