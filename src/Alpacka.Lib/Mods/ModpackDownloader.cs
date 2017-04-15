using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alpacka.Lib.Config;
using Alpacka.Lib.Net;

namespace Alpacka.Lib.Mods
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
            
            var processed  = new List<ModWrapper>();
            var processing = config.Mods.Select(mod => new ModWrapper(mod.Clone(), _sources)).ToList();
            FireDownloaderExceptionIfErrored(processing, "parsing mod sources");
            
            var byName  = new Dictionary<string, ModWrapper>();
            var byModID = new Dictionary<string, ModWrapper>();
            
            bool IsPresent(ModWrapper mod) {
                var name  = mod.ComparableName;
                var modID = mod.ModInfo?.ModID;
                return (!string.IsNullOrEmpty(name) && byName.ContainsKey(name)) ||
                       ((modID != null) && byModID.ContainsKey(modID));
            }
            
            var dependencies = new List<EntryMod>();
            Action<EntryMod> addDependency = (dependency) =>
                { lock (dependencies) dependencies.Add(dependency); };
            
            while (processing.Count > 0) {
                await Task.WhenAll(processing.Select(mod => {
                    // If mod version is not set, set it to the config default now (recommended or latest).
                    if (mod.Mod.Version == null) mod.Mod.Version =
                        config.Defaults.Version.ToString().ToLowerInvariant();
                    return mod.Resolve(config.MinecraftVersion, addDependency);
                }));
                FireDownloaderExceptionIfErrored(processing, "resolving mods");
                
                // Discard mods whose download URL has not been set,
                // as well as dependencies that have already been downloaded.
                processing = processing.Where(mod =>
                        (mod.DownloadURL != null) &&
                        !(IsPresent(mod) && mod.IsDependency)
                    ).ToList();
                
                await Task.WhenAll(processing.Select(mod => mod.Download(_fileDownloader)));
                FireDownloaderExceptionIfErrored(processing, "downloading mods");
                await Task.WhenAll(processing.Select(mod => mod.ExtractModInfo()));
                FireDownloaderExceptionIfErrored(processing, "extracting mcmod.info");
                
                foreach (var mod in processing) {
                    // TODO: If mod is already present, merge properties such as Side information.
                    if (IsPresent(mod) && mod.IsDependency) continue;
                    
                    var name  = mod.ComparableName;
                    var modID = mod.ModInfo?.ModID;
                    if (!string.IsNullOrEmpty(name)) byName.Add(name, mod);
                    if (modID != null) byModID.Add(modID, mod);
                    
                    processed.Add(mod);
                }
                
                processing = dependencies
                    .Select(mod => new ModWrapper(mod, _sources, true))
                    .ToList();
                dependencies.Clear();
            }
            
            return new ModpackBuild(config) {
                ForgeVersion = forgeVersion,
                Mods = processed.Select(mod => {
                        mod.Mod.Source = mod.DownloadURL;
                        return mod.Mod;
                    }).ToList()
            };
        }
        
        /// <summary> Downloads all mods from the specified ModpackBuild. </summary>
        public Task<List<DownloadedMod>> Download(ModpackBuild build) => // TODO: Filter side?
            Task.WhenAll(build.Mods.Select(mod =>
                    _fileDownloader.Download(mod.Source)
                        .ContinueWith(task => new DownloadedMod(mod, task.Result))
                )).ContinueWith(task => new List<DownloadedMod>(task.Result));
        
        
        private void FireDownloaderExceptionIfErrored(List<ModWrapper> mods, string message)
        {
            var errored = mods
                .Where(mod => (mod.Exception != null))
                .Select(mod => Tuple.Create(mod.Mod, mod.Exception))
                .ToList();
            if (errored.Count > 0) throw new DownloaderException(message, errored);
        }
        
        
        private class ModWrapper
        {
            public EntryMod Mod { get; }
            public bool IsDependency { get; }
            public IModSource SourceHandler { get; }
            
            public string DownloadURL { get; private set; }
            public DownloadedFile DownloadedFile { get; private set; }
            public MCModInfo ModInfo { get; private set; }
            
            public Exception Exception { get; private set; }
            
            private static readonly Regex _modNameComparisonFilter =
                new Regex(@"\W", RegexOptions.CultureInvariant);
            public string ComparableName { get {
                if (Mod.Name == null) return null;
                var name = _modNameComparisonFilter.Replace(Mod.Name, "").ToLowerInvariant();
                return (name.Length > 0) ? name : Mod.Name;
            } }
            
            internal ModWrapper(EntryMod mod, List<IModSource> sources, bool isDependency = false) {
                Mod = mod;
                IsDependency = isDependency;
                SourceHandler = sources.Find(src => src.CanHandle(mod.Source));
                if (SourceHandler == null)
                    Exception = new Exception("No source handling for '{ mod.Source }'");
            }
            
            public async Task Resolve(string mcVersion, Action<EntryMod> addDependency)
            {
                try { DownloadURL = await SourceHandler.Resolve(Mod, mcVersion, addDependency); }
                catch (Exception ex) { Exception = ex; }
            }
            
            public async Task Download(IFileDownloader fileDownloader)
            {
                try { DownloadedFile = await fileDownloader.Download(DownloadURL); }
                catch (Exception ex) { Exception = ex; }
            }
            
            public async Task ExtractModInfo()
            {
                using (var readStream = File.OpenRead(DownloadedFile.Path)) {
                    try { ModInfo = await MCModInfo.Extract(readStream); }
                    catch (MCModInfoException ex) {
                        Console.WriteLine($"INFO: Could not read mcmod.info of mod '{ this }': { ex.Message }");
                        return;
                    }
                }
                
                if (string.IsNullOrEmpty(Mod.Name)) Mod.Name = ModInfo.Name;
                if (string.IsNullOrEmpty(Mod.Description)) Mod.Description = ModInfo.Description;
                if (string.IsNullOrEmpty(Mod.Links?.Website)) {
                    if (Mod.Links == null) Mod.Links = new EntryLinks();
                    Mod.Links.Website = ModInfo.URL;
                }
                
                // Some mods don't replace their version string correctly.
                if (ModInfo.Version != "@VERSION@") Mod.Version = ModInfo.Version;
                // TODO: Warn if version doesn't match up?
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
        public DownloaderException(string hint, List<Tuple<EntryMod, Exception>> exceptions)
            : base(CreateMessage(hint, exceptions)) {  }
        
        private static string CreateMessage(string hint, List<Tuple<EntryMod, Exception>> exceptions) =>
            $"{ exceptions.Count } error{ ((exceptions.Count != 1) ? "s" : "") } occured while { hint }:\n" +
                string.Join("", exceptions.Select(tuple =>
                    $"\nFor mod { tuple.Item1.Name } (src: '{ tuple.Item1.Source }'):\n" +
                    $"  { tuple.Item2.ToString().Replace("\n", "  \n") }\n"));
    }
}
