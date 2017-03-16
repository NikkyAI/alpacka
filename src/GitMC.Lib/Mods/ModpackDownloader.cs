using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public class ModpackDownloader : IEnumerable<IModSource>
    {
        private readonly ModpackConfig _config;
        private readonly List<IModSource> _sources = new List<IModSource>();
        
        public ModpackDownloader(ModpackConfig config)
        {
            _config = config;
        }
        
        public void Add(IModSource source) => _sources.Add(source);
        
        public async Task<List<DownloadedMod>> Run()
        {
            var processingMods = _config.Mods;
            var idToModDict    = new Dictionary<string, ModWrapper>();
            var dependencies   = new List<EntryMod>();
            
            Action<EntryMod> addDependency = (dependency) =>
                { lock (dependencies) dependencies.Add(dependency); };
            
            while (processingMods.Count > 0) {
                var isHandlingDependencies = (processingMods != _config.Mods); // Might be useful later.
                var mods = processingMods.Select(mod =>
                    new ModWrapper(mod, _sources.Find(source =>
                        source.CanHandle(mod.Scheme)))).ToList();
                
                // See if any if the mods don't have a mod source handler.
                var noSources = mods.Where(mod => (mod.Source == null)).Select(mod => mod.Mod).ToList();
                if (noSources.Count > 0) throw new NoSourceHandlerException(noSources);
                
                await Task.WhenAll(mods.Select(mod => mod.Resolve(_config.MinecraftVersion, addDependency)));
                await Task.WhenAll(mods.Select(mod => mod.Download()));
                await Task.WhenAll(mods.Select(mod => mod.ExtractModInfo()));
                
                // TODO: Handle the possibility of knowing ModID before mod has been downloaded completely.
                // TODO: If mod already exists in idToModDict, merge properties such as Side information.
                foreach (var mod in mods)
                    if (!idToModDict.ContainsKey(mod.ModInfo.ModID))
                        idToModDict.Add(mod.ModInfo.ModID, mod);
                
                processingMods = dependencies;
                dependencies   = new List<EntryMod>();
            }
            
            return idToModDict.Values.Select(mod =>
                new DownloadedMod(mod.Mod, mod.TempPath, mod.FileName)).ToList();
        }
        
        public class DownloadedMod
        {
            public EntryMod Mod { get; }
            public string TempPath { get; }
            public string FileName { get; }
            public DownloadedMod(EntryMod mod, string tempPath, string fileName)
                { Mod = mod; TempPath = tempPath; FileName = fileName; }
        }
        
        private class ModWrapper
        {
            public EntryMod Mod { get; }
            public IModSource Source { get; }
            public string TempPath { get; private set; }
            public string FileName { get; private set; }
            public MCModInfo ModInfo { get; private set; }
            
            public ModWrapper(EntryMod mod, IModSource source)
                { Mod = mod; Source = source; }
            
            public Task Resolve(string mcVersion, Action<EntryMod> addDependency) =>
                Source.Resolve(Mod, mcVersion, addDependency);
            
            public async Task Download()
            {
                TempPath = Path.GetTempFileName();
                var md5 = new MD5Transform();
                using (var writeStream = new CryptoStream(File.OpenWrite(TempPath), md5, CryptoStreamMode.Write))
                    FileName = await Source.Download(Mod, writeStream);
                var hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
                Console.WriteLine($"Downloaded '{ this }' to { TempPath } (MD5: { hash })");
                
                if ((Mod.MD5 != null) && !string.Equals(Mod.MD5, hash, StringComparison.OrdinalIgnoreCase))
                    throw new DownloaderException($"MD5 hash of '{ this }' ({ hash }) does not match provided MD5 hash ({ Mod.MD5 }) in config");
                else Mod.MD5 = hash;
            }
            
            public async Task ExtractModInfo()
            {
                try { using (var readStream = File.OpenRead(TempPath))
                    ModInfo = await MCModInfo.Extract(readStream); }
                catch (Exception ex) { throw new DownloaderException(
                    $"Exception when extracting mcmod.info data for mod '{ this }'", ex); }
                // TODO: Gracefully handle missing / invalid mcmod.info and allow specifying stuff manually.
                
                if (Mod.Name == null) Mod.Name = ModInfo.Name;
                if (Mod.Description == null) Mod.Description = ModInfo.Description;
                if (Mod.Version == null) Mod.Version = ModInfo.Version;
                if ((Mod.Links == null) && !string.IsNullOrEmpty(ModInfo.URL))
                    Mod.Links = new EntryLinks { Website = ModInfo.URL };
                // TODO: Warn if version doesn't match up?
                
                Console.WriteLine($"Extracted mod info :: Name: { ModInfo.Name } - Version: { ModInfo.Version }");
            }
            
            public override string ToString() => (Mod.Name ?? Mod.Source);
        }
        
        // IEnumerable implementation
        // This is required to use the collection initializer.
        IEnumerator<IModSource> IEnumerable<IModSource>.GetEnumerator() => _sources.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _sources.GetEnumerator();
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
            return "No scheme handling for mod sources:\n  " +
                string.Join("\n  ", mods.Select(mod => {
                    var scheme = (mod.Scheme != null) ? $"'{ mod.Scheme }'" : "(none)";
                    return $"Scheme: { scheme } - Source: '{ mod.Source }'";
                }));
        }
    }
}
