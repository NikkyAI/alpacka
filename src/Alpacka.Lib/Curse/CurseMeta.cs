using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Alpacka.Lib.Net;

namespace Alpacka.Lib.Curse
{
    public class CurseMeta : IDisposable
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Ignore 
            };
        
        private FileCache _cache;
        private readonly FileDownloader _downloader;
        private static readonly string URL_BASE = "https://cursemeta.nikky.moe";
        
        private bool _disposed = false;
        
        public CurseMeta() {
            _cache = new FileCache(Path.Combine(Constants.CachePath,"cursemeta"));
            _downloader = new FileDownloader(_cache);
        }
        
        ~CurseMeta() =>
            Dispose();
            
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cache.Dispose();
            _downloader.Dispose();
        }
        
        // Get Addon
        private readonly ConcurrentDictionary<int, Task<Addon>> _addonTasks =
            new ConcurrentDictionary<int, Task<Addon>>();
        
        public Task<Addon> GetAddon(int addonId) =>
            _addonTasks.GetOrAdd(addonId, id => GetAddonInternal(id));
        
        private async Task<Addon> GetAddonInternal(int addonId)
        {
            Debug.WriteLine($"getAddon { addonId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/index.json", $"addon/{ addonId }/index.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<Addon>(json, _settings);
        }
        
        // Get Addon Description
        
        private readonly ConcurrentDictionary<int, Task<string>> _addonDescriptionTasks =
            new ConcurrentDictionary<int, Task<string>>();
        
        public Task<string> GetAddonDescription(int addonId) =>
            _addonDescriptionTasks.GetOrAdd(addonId, id => GetAddonDescriptionInternal(id));
        
        private async Task<string> GetAddonDescriptionInternal(int addonId)
        {
            Debug.WriteLine($"getAddonDescption { addonId }"); // TODO: verbose logging
            var htmlFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/description.html", $"addon/{ addonId }/description.html");
            return File.ReadAllText(htmlFile.FullPath);
        }
        
        // Get Addon Files
        
        private readonly ConcurrentDictionary<int, Task<AddonFile[]>> _addonFilesTasks =
            new ConcurrentDictionary<int, Task<AddonFile[]>>();
        
        public Task<AddonFile[]> GetAddonFiles(int addonId) =>
            _addonFilesTasks.GetOrAdd(addonId, id => GetAddonFilesInternal(id));
        
        private async Task<AddonFile[]> GetAddonFilesInternal(int addonId)
        {
            Debug.WriteLine($"getAddonFiles { addonId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/index.json", $"addon/{ addonId }/files/index.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<AddonFile[]>(json, _settings);
        }
        
        // Get Addon File
        
        private readonly ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>> _addonFileTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>>();
        
        public Task<AddonFile> GetAddonFile(int addonId, int fileId) =>
            _addonFileTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileInternal(id.Item1, id.Item2));
        
        private async Task<AddonFile> GetAddonFileInternal(int addonId, int fileId) 
        {
            Debug.WriteLine($"getAddonFile { addonId } { fileId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/{ fileId }.json", $"addon/{ addonId }/files/{ fileId }.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<AddonFile>(json, _settings);
        }
        
        // Get Addon File Changelog
        
        private readonly ConcurrentDictionary<Tuple<int, int>, Task<string>> _addonFileChangelogTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<string>>();

        public Task<string> GetAddonFileChangelog(int addonId, int fileId) =>
            _addonFileChangelogTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileChangelogInternal(id.Item1, id.Item2));
        
        private async Task<string> GetAddonFileChangelogInternal(int addonId, int fileId) 
        {
            Debug.WriteLine($"getAddonFileChangelog { addonId } { fileId }"); // TODO: verbose logging
            var htmlFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/{ fileId }.changelog.html", $"addon/{ addonId }/files/{ fileId }.changelog.html");
            return File.ReadAllText(htmlFile.FullPath);
        }
    }
}
