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
    public class ModpackDownloader : IDependencyHandler, IEnumerable<IModSource>
    {
        private readonly ModpackConfig _config;
        private readonly List<IModSource> _sources = new List<IModSource>();
        
        public ModpackDownloader(ModpackConfig config)
        {
            _config = config;
        }
        
        public void Add(IModSource source) => _sources.Add(source);
        
        public async Task Run()
        {
            var mods = _config.Mods.Select(mod =>
                new ModWrapper(mod, _sources.Find(source =>
                    source.CanHandle(mod.Scheme)))).ToList();
            
            // See if any if the mods don't have a mod source handler.
            var noSources = mods.Where(mod => (mod.Source == null)).Select(mod => mod.Mod).ToList();
            if (noSources.Count > 0) throw new NoSourceHandlerException(noSources);
            
            await Task.WhenAll(mods.Select(mod => mod.Resolve(_config.MinecraftVersion, this)));
            await Task.WhenAll(mods.Select(mod => mod.Download()));
            await Task.WhenAll(mods.Select(mod => mod.ExtractModInfo()));
        }
        
        private class ModWrapper
        {
            public EntryMod Mod { get; }
            public IModSource Source { get; }
            public string TempPath { get; private set; }
            public MCModInfo ModInfo { get; private set; }
            
            public ModWrapper(EntryMod mod, IModSource source)
                { Mod = mod; Source = source; }
            
            public Task Resolve(string mcVersion, IDependencyHandler dependencies) =>
                Source.Resolve(Mod, mcVersion, dependencies);
            
            public async Task Download()
            {
                TempPath = Path.GetTempFileName();
                var md5 = new MD5Transform();
                using (var writeStream = new CryptoStream(File.OpenWrite(TempPath), md5, CryptoStreamMode.Write))
                    await Source.Download(Mod, writeStream);
                var hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
                Console.WriteLine($"Downloaded '{ Mod.Source }' to { TempPath } (MD5: { hash })");
                
                if ((Mod.MD5 != null) && !string.Equals(Mod.MD5, hash, StringComparison.OrdinalIgnoreCase))
                    throw new DownloaderException($"MD5 hash of '{ Mod.Source }' ({ hash }) does not match provided MD5 hash ({ Mod.MD5 }) in config");
                else Mod.MD5 = hash;
            }
            
            public async Task ExtractModInfo()
            {
                using (var readStream = File.OpenRead(TempPath))
                    ModInfo = await MCModInfo.Extract(readStream);
                if (Mod.Name == null) Mod.Name = ModInfo.Name;
                if (Mod.Description == null) Mod.Description = ModInfo.Description;
                if (Mod.Version == null) Mod.Version = ModInfo.Version;
                if ((Mod.Links == null) && !string.IsNullOrEmpty(ModInfo.URL))
                    Mod.Links = new EntryLinks { Website = ModInfo.URL };
                Console.WriteLine($"Extracted mod info :: Name: { ModInfo.Name } - Version: { ModInfo.Version }");
            }
        }
        
        void IDependencyHandler.Add(EntryMod dependency) =>
            throw new NotImplementedException();
        
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
