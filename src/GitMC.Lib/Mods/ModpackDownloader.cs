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
        
        public ModpackDownloader(IFileDownloader fileDownloader = null)
        {
            _fileDownloader = fileDownloader
                ?? new FileDownloaderURL(Constants.ModsCache);
        }
        
        ~ModpackDownloader() => Dispose();
        
        public void Dispose()
        {
            (_fileDownloader as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
        
        public ModpackDownloader WithSourceHandler(IModSource source)
            { _sources.Add(source); return this; }
        
        public async Task<List<DownloadedMod>> Run(ModpackVersion pack)
        {
            var processingMods = pack.Mods;
            var modDict        = new Dictionary<string, ModWrapper>();
            var dependencies   = new List<EntryMod>();
            
            Action<EntryMod> addDependency = (dependency) =>
                { lock (dependencies) dependencies.Add(dependency); };
            
            while (processingMods.Count > 0) {
                var isHandlingDependencies = (processingMods != pack.Mods); // Might be useful later.
                var mods = processingMods.Select(mod => new ModWrapper(mod, _sources)).ToList();
                
                // See if any if the mods don't have a mod source handler.
                var noSources = mods.Where(mod => (mod.SourceHandler == null)).Select(mod => mod.Mod).ToList();
                if (noSources.Count > 0) throw new NoSourceHandlerException(noSources);
                
                await Task.WhenAll(mods.Select(mod => mod.Resolve(pack.MinecraftVersion, addDependency)));
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
                
                processingMods = dependencies;
                dependencies   = new List<EntryMod>();
            }
            
            return modDict.Values.Select(mod => {
                // Replace Source with resolved download URL before returning.
                mod.Mod.Source = mod.DownloadURL;
                return new DownloadedMod(mod.Mod, mod.DownloadedFile);
            }).ToList();
        }
        
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
                try { using (var readStream = File.OpenRead(DownloadedFile.Path))
                    ModInfo = await MCModInfo.Extract(readStream); }
                catch (Exception ex) {
                    Console.WriteLine($"Warning: Couldn't load mcmod.info of '{ this }': { ex.Message }");
                    return;
                }
                
                if (string.IsNullOrEmpty(Mod.Name)) Mod.Name = ModInfo.Name;
                if (string.IsNullOrEmpty(Mod.Description)) Mod.Description = ModInfo.Description;
                if (string.IsNullOrEmpty(Mod.Version)) Mod.Version = ModInfo.Version;
                if (string.IsNullOrEmpty(Mod.Links?.Website)) {
                    if (Mod.Links == null) Mod.Links = new EntryLinks();
                    Mod.Links.Website = ModInfo.URL;
                }
                // TODO: Warn if version doesn't match up?
            }
            
            public override string ToString() => (Mod.Name ?? Mod.Source);
        }
    }
    
    public class DownloadedMod
    {
        public EntryMod Mod { get; }
        public DownloadedFile File { get; }
        
        public DownloadedMod(EntryMod mod, DownloadedFile file) { Mod = mod; File = file; }
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
