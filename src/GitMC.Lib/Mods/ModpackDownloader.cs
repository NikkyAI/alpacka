using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMC.Lib.Config;
using GitMC.Lib.Net;

namespace GitMC.Lib.Mods
{
    public class ModpackDownloader : IDisposable
    {
        private readonly IFileDownloader _fileDownloader;
        private readonly List<IModSource> _sources = new List<IModSource>();
        
        public ModpackDownloader(FileCache modsCache)
            : this(new FileDownloaderURL(modsCache)) {  }
        public ModpackDownloader(IFileDownloader fileDownloader)
            { _fileDownloader = fileDownloader; }
        
        ~ModpackDownloader() => Dispose();
        
        public void Dispose()
        {
            (_fileDownloader as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
        
        public ModpackDownloader WithSourceHandler(IModSource source)
            { _sources.Add(source); return this; }
        
        
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
            
            var processing = config.Mods.Select(mod => mod.Clone()).ToList();
            var modDict    = new Dictionary<string, ModWrapper>();
            
            var dependencies = new List<EntryMod>();
            Action<EntryMod> addDependency = (dependency) =>
                { lock (dependencies) dependencies.Add(dependency); };
            
            while (processing.Count > 0) {
                var mods = processing.Select(mod => new ModWrapper(mod, _sources)).ToList();
                
                // See if any if the mods don't have a mod source handler.
                var noSources = mods.Where(mod => (mod.SourceHandler == null)).Select(mod => mod.Mod).ToList();
                if (noSources.Count > 0) throw new NoSourceHandlerException(noSources);
                
                await Task.WhenAll(mods.Select(mod => {
                    // If mod version is not set, set it to the config default now (recommended or latest).
                    if (mod.Mod.Version == null) mod.Mod.Version =
                        config.Defaults.Version.ToString().ToLowerInvariant();
                    return mod.Resolve(config.MinecraftVersion, addDependency);
                }));
                // Discard mods whose download URL has not been set.
                mods = mods.Where(mod => (mod.DownloadURL != null)).ToList();
                
                await Task.WhenAll(mods.Select(mod => mod.Download(_fileDownloader)));
                await Task.WhenAll(mods.Select(mod => mod.ExtractModInfo()));
                
                // TODO: Handle the possibility of knowing ModID before mod has been downloaded completely.
                // TODO: If mod already exists in modDict, merge properties such as Side information.
                foreach (var mod in mods) {
                    // Unfortunately, not every mod contains an mcmod.info,
                    // in that case just use the Source as dictionary key.
                    var key = mod.ModInfo?.ModID ?? mod.Mod.Source;
                    if (!modDict.ContainsKey(key)) modDict.Add(key, mod);
                    else Console.WriteLine($"Debug: Downloaded '{ mod }' multiple times");
                }
                
                processing   = dependencies;
                dependencies = new List<EntryMod>();
            }
            
            foreach (var mod in modDict.Values)
                mod.Mod.Source = mod.DownloadURL;
            
            return new ModpackBuild(config) {
                ForgeVersion = forgeVersion,
                Mods = modDict.Values.Select(mod => mod.Mod).ToList()
            };
        }
        
        /// <summary> Downloads all mods from the specified ModpackBuild. </summary>
        public Task<List<DownloadedMod>> Download(ModpackBuild build) => // TODO: Filter side?
            Task.WhenAll(build.Mods.Select(mod =>
                    _fileDownloader.Download(mod.Source)
                        .ContinueWith(task => new DownloadedMod(mod, task.Result))
                )).ContinueWith(task => new List<DownloadedMod>(task.Result));
        
        
        private class ModWrapper
        {
            public EntryMod Mod { get; }
            public IModSource SourceHandler { get; }
            
            public string DownloadURL { get; private set; }
            public DownloadedFile DownloadedFile { get; private set; }
            public MCModInfo ModInfo { get; private set; }
            
            internal ModWrapper(EntryMod mod, List<IModSource> sources)
                { Mod = mod; SourceHandler = sources.Find(src => src.CanHandle(mod.Source)); }
            
            public async Task Resolve(string mcVersion, Action<EntryMod> addDependency) =>
                DownloadURL = await SourceHandler.Resolve(Mod, mcVersion, addDependency);
            
            public async Task Download(IFileDownloader fileDownloader) =>
                DownloadedFile = await fileDownloader.Download(DownloadURL);
            
            public async Task ExtractModInfo()
            {
                try {
                    using (var readStream = File.OpenRead(DownloadedFile.Path))
                        ModInfo = await MCModInfo.Extract(readStream);
                } catch (Exception ex) {
                    Console.WriteLine($"Warning: Couldn't load mcmod.info of '{ this }': { ex.Message }");
                    return;
                }
                
                if (string.IsNullOrEmpty(Mod.Name)) Mod.Name = ModInfo.Name;
                if (string.IsNullOrEmpty(Mod.Description)) Mod.Description = ModInfo.Description;
                Mod.Version = ModInfo.Version; // TODO: Warn if version doesn't match up?
                if (string.IsNullOrEmpty(Mod.Links?.Website)) {
                    if (Mod.Links == null) Mod.Links = new EntryLinks();
                    Mod.Links.Website = ModInfo.URL;
                }
            }
            
            public override string ToString() => (Mod.Name ?? Mod.Source);
        }
    }
    
    public class DownloadedMod
    {
        public EntryMod Mod { get; }
        public DownloadedFile File { get; }
        
        public DownloadedMod(EntryMod mod, DownloadedFile file)
            { Mod = mod; File = file; }
    }
    
    public class DownloaderException : Exception
    {
        public DownloaderException(string message)
            : base(message) {  }
        public DownloaderException(string message, Exception innerException)
            : base(message, innerException) {  }
    }
    
    public class NoSourceHandlerException : DownloaderException
    {
        public NoSourceHandlerException(List<EntryMod> mods)
            : base(CreateMessage(mods)) {  }
        
        private static string CreateMessage(List<EntryMod> mods)
        {
            return "No source handling for mods:\n  " +
                string.Join("\n  ", mods.Select(mod => $"Source: '{ mod.Source }'"));
        }
    }
}
