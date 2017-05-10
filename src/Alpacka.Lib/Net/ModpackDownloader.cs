using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Pack.Config;
using Alpacka.Lib.Resources;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Net
{
    public class ModpackDownloader : IDisposable
    {
        private readonly FileCache _fileCache;
        private readonly FileDownloader _fileDownloader;
        
        public ModpackDownloader()
        {
            _fileCache = new FileCache(Path.Combine(Constants.CachePath, "resources"));
            _fileDownloader = new FileDownloader(_fileCache);
        }
        
        ~ModpackDownloader() => Dispose();
        
        public void Dispose()
        {
            (_fileDownloader as IDisposable)?.Dispose();
            (_fileCache as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
        
        /// <summary> Resolves the mods from the specified ModpackConfig and creates a
        ///           ModpackBuild with all dependencies and sources set to http(s) URLs. </summary>
        public async Task<ModpackBuild> Resolve(ModpackConfig config) // TODO: Filter side?
        {
            var forgeVersion = config.ForgeVersion;
            // If forge version is "recommended" or "latest", grab the latest forge version.
            Release recommendation;
            if (Enum.TryParse(forgeVersion, true, out recommendation))
                forgeVersion = (await ForgeVersionData.Download())
                    .GetRecent(config.MinecraftVersion, recommendation)
                    .GetFullVersion();
            
            var resources = new List<ResourceWrapper>();
            var modByName = new Dictionary<string, ResourceWrapper>();
            var modByID   = new Dictionary<string, ResourceWrapper>();
            
            // TODO: Handle features.
            void ParseGroup(EntryIncludes.Group group, EntryDefaults.Group defaults)
            {
                // If the group contains any names ("foogroup & client & optional") which
                // are defined in the config Defaults, merge them into the current defaults.
                foreach (var name in group.Names) {
                    defaults += config.Defaults[name];
                    if (AlpackaRegistry.ResourceHandlers[defaults.Handler] == null)
                        throw new Exception($"Invalid resource handler '{ defaults.Handler }'");
                }
                
                foreach (var element in group) {
                    var subGroup = element as EntryIncludes.Group;
                    if (subGroup != null) ParseGroup(subGroup, defaults);
                    else resources.Add(new ResourceWrapper((EntryResource)element, defaults));
                }
            }
            
            foreach (var group in config.Includes)
                ParseGroup(group, new EntryDefaults.Group());
            FireDownloaderExceptionIfErrored(resources, "parsing mod sources");
            
            // Initialize used source handlers.
            await Task.WhenAll(resources
                .Select(resource => resource.Handler).Distinct()
                .Select(handler => handler.Initialize()));
            
            var startIndex = 0;
            while (startIndex < resources.Count) {
                var count = resources.Count - startIndex;
                var range = Enumerable.Range(startIndex, count).Select(i => resources[i]);
                startIndex += count;
                
                await Task.WhenAll(range.Select(res => res.Resolve(config.MinecraftVersion, resources)));
                FireDownloaderExceptionIfErrored(range, "resolving mods");
                await Task.WhenAll(range.Select(res => res.Download(_fileDownloader)));
                FireDownloaderExceptionIfErrored(range, "downloading mods");
                await Task.WhenAll(range.Select(res => res.ExtractModInfo()));
                FireDownloaderExceptionIfErrored(range, "extracting mcmod.info");
                
                foreach (var resource in range) {
                    // GetComparableName and ModID return null for non-mods.
                    var name  = resource.GetComparableName();
                    var modID = resource.ModID;
                    var hasName  = !string.IsNullOrEmpty(name);
                    var hasModID = (modID != null);
                    
                    // Remove any duplicate mods.
                    if (resource.IsDependency &&
                        ((hasName && modByName.ContainsKey(name)) ||
                         (hasModID && modByID.ContainsKey(modID)))) {
                        // TODO: If mod is already present, merge properties such as Side information.
                        resource.MarkAsRemoved();
                    } else {
                        if (hasName) modByName.Add(name, resource);
                        if (hasModID) modByID.Add(modID, resource);
                        
                    }
                }
            }
            
            var build = ModpackBuild.CopyFrom(config);
            build.ForgeVersion = forgeVersion;
            build.Mods = resources
                .Select(wrapper => wrapper.Resource)
                .OfType<EntryMod>().ToList();
            // TODO: Resources.
            // TODO: Features.
            
            return build;
        }
        
        private void FireDownloaderExceptionIfErrored(
            IEnumerable<ResourceWrapper> resources, string message)
        {
            var errored = resources
                .Where(res => (res.Exception != null))
                .Select(res => Tuple.Create(res.Resource, res.Exception))
                .ToList();
            if (errored.Count > 0) throw new DownloaderException(message, errored);
        }
        
        /// <summary> Downloads all mods from the specified ModpackBuild. </summary>
        public Task<List<DownloadedResource>> Download(ModpackBuild build) => // TODO: Filter side?
            Task.WhenAll(build.Mods.Select(mod =>
                    _fileDownloader.Download(mod.Source)
                        .ContinueWith(task => new DownloadedResource(mod, task.Result))
                )).ContinueWith(task => new List<DownloadedResource>(task.Result));
        
        
        private class ResourceWrapper
        {
            public bool IsDependency { get; }
            public IResourceHandler Handler { get; }
            
            public EntryResource Resource { get; private set; }
            public DownloadedFile DownloadedFile { get; private set; }
            
            public string ModID { get; private set; }
            public Exception Exception { get; private set; }
            public bool Valid => (Resource != null);
            
            internal ResourceWrapper(EntryResource resource, EntryDefaults.Group defaults)
                : this(resource, defaults, false) {  }
            internal ResourceWrapper(EntryResource resource)
                : this(resource, null, true) {  }
            private ResourceWrapper(EntryResource resource, EntryDefaults.Group defaults, bool isDependency)
            {
                Resource = resource.Clone();
                Resource.Handler = Resource.Handler ?? defaults?.Handler;
                Resource.Version = Resource.Version ?? defaults?.Version.ToString().ToLowerInvariant();
                Resource.Path = Resource.Path ?? defaults?.Path;
                Resource.Side = Resource.Side ?? defaults?.Side;
                IsDependency = isDependency;
                
                Handler = AlpackaRegistry.ResourceHandlers.FirstOrDefault(
                        handler => handler.ShouldOverwriteHandler(Resource.Source))
                    ?? AlpackaRegistry.ResourceHandlers[Resource.Handler];
                if (Handler == null) Exception = new Exception("No handler found");
            }
            
            public async Task Resolve(string mcVersion, List<ResourceWrapper> resources)
            {
                try {
                    Resource = await Handler.Resolve(Resource, mcVersion, (dependency) => {
                        lock (resources) { resources.Add(new ResourceWrapper(dependency)); } });
                    Resource.Handler = null;
                } catch (Exception ex) { Exception = ex; }
            }
            
            public async Task Download(FileDownloader fileDownloader)
            {
                if (Resource == null) return;
                try {
                    DownloadedFile = await fileDownloader.Download(Resource.Source);
                    // If the downloaded file is a .jar file and Resource is not yet an EntryMod, convert it.
                    if (!(Resource is EntryMod) && Path.GetExtension(DownloadedFile.FileName)
                        .Equals(".jar", StringComparison.OrdinalIgnoreCase))
                        {
                            Resource = EntryMod.Convert(Resource);
                            Resource.Path = Path.Combine(Resource.Path, DownloadedFile.FileName);
                        }
                } catch (Exception ex) { Exception = ex; }
            }
            
            public async Task ExtractModInfo()
            {
                var mod = (Resource as EntryMod);
                if (mod == null) return;
                
                MCModInfo modInfo;
                using (var readStream = File.OpenRead(DownloadedFile.FullPath))
                    try { modInfo = await MCModInfo.Extract(readStream); }
                    catch (MCModInfoException ex) {
                        Console.WriteLine($"INFO: Could not read mcmod.info of mod { this }: { ex.Message }");
                        return;
                    }
                
                // Apparently some mods don't update their modid in mcmod.info from "examplemod".
                if (modInfo.ModID == "examplemod")
                    Console.WriteLine($"INFO: ModID in mcmod.info of mod { this } is '{ modInfo.ModID }'");
                else ModID = modInfo.ModID;
                
                if (string.IsNullOrEmpty(mod.Name)) mod.Name = modInfo.Name;
                if (string.IsNullOrEmpty(mod.Description)) mod.Description = modInfo.Description;
                if (string.IsNullOrEmpty(mod.Links?.Website)) {
                    if (mod.Links == null) mod.Links = new EntryLinks();
                    mod.Links.Website = modInfo.URL;
                }
                
                if (!string.IsNullOrEmpty(modInfo.Version)) {
                    // Some mods don't replace their version string correctly.
                    var chr = modInfo.Version[0];
                    if ((chr == '$') || (chr == '@'))
                        Console.WriteLine($"INFO: Version in mcmod.info of mod { this } is '{ modInfo.Version }'");
                    else mod.Version = modInfo.Version;
                }
                // TODO: Warn if version doesn't match up?
            }
            
            public void MarkAsRemoved() =>
                Resource = null;
            
            private static readonly Regex _modNameComparisonFilter =
                new Regex(@"\W", RegexOptions.CultureInvariant);
            public string GetComparableName()
            {
                var mod = (Resource as EntryMod);
                if (mod?.Name == null) return null;
                var name = _modNameComparisonFilter.Replace(mod.Name, "").ToLowerInvariant();
                return (name.Length > 0) ? name : mod.Name;
            }
            
            public override string ToString()
            {
                var mod = (Resource as EntryMod);
                return !string.IsNullOrEmpty(mod?.Name)
                    ? $"{ mod.Name } (Source: '{ mod.Source }')"
                    : $"'{ Resource.Source }'";
            }
        }
    }
    
    public class DownloadedResource
    {
        public EntryResource Resource { get; }
        public DownloadedFile File { get; }
        
        public DownloadedResource(EntryResource resource, DownloadedFile file)
            { Resource = resource; File = file; }
    }
    
    public class DownloaderException : Exception
    {
        public DownloaderException(string hint, List<Tuple<EntryResource, Exception>> exceptions)
            : base(CreateMessage(hint, exceptions)) {  }
        
        private static string CreateMessage(string hint, List<Tuple<EntryResource, Exception>> exceptions) =>
            $"{ exceptions.Count } error{ ((exceptions.Count != 1) ? "s" : "") } occured while { hint }:\n" +
                string.Join("", exceptions.Select(tuple =>
                    $"\nFor resource { tuple.Item1 }:\n" +
                    $"  { tuple.Item2.ToString().Replace("\n", "  \n") }\n"));
    }
}
