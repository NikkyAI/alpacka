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
    public static class CurseMeta
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Ignore 
            };
        
        private static readonly FileDownloaderURL _downloader = 
            new FileDownloaderURL(new FileCache(Path.Combine(Constants.CachePath, "cursemeta")));
        private static readonly string URL_BASE = "https://cursemeta.nikky.moe";
        
        // Get Addon
        private static readonly ConcurrentDictionary<int, Task<Addon>> _addonTasks =
            new ConcurrentDictionary<int, Task<Addon>>();
        
        public static Task<Addon> GetAddon(int addonId) =>
            _addonTasks.GetOrAdd(addonId, id => GetAddonInternal(id));
        
        private static async Task<Addon> GetAddonInternal(int addonId)
        {
            Debug.WriteLine($"getAddon { addonId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/index.json", $"addon/{ addonId }/index.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<Addon>(json, _settings);
        }
        
        // Get Addon Description
        
        private static readonly ConcurrentDictionary<int, Task<string>> _addonDescriptionTasks =
            new ConcurrentDictionary<int, Task<string>>();
        
        public static Task<string> GetAddonDescription(int addonId) =>
            _addonDescriptionTasks.GetOrAdd(addonId, id => GetAddonDescriptionInternal(id));
        
        private static async Task<string> GetAddonDescriptionInternal(int addonId)
        {
            Debug.WriteLine($"getAddonDescption { addonId }"); // TODO: verbose logging
            var htmlFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/description.html", $"addon/{ addonId }/description.html");
            return File.ReadAllText(htmlFile.FullPath);
        }
        
        // Get Addon Files
        
        private static readonly ConcurrentDictionary<int, Task<AddonFile[]>> _addonFilesTasks =
            new ConcurrentDictionary<int, Task<AddonFile[]>>();
        
        public static Task<AddonFile[]> GetAddonFiles(int addonId) =>
            _addonFilesTasks.GetOrAdd(addonId, id => GetAddonFilesInternal(id));
        
        private static async Task<AddonFile[]> GetAddonFilesInternal(int addonId)
        {
            Debug.WriteLine($"getAddonFiles { addonId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/index.json", $"addon/{ addonId }/files/index.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<AddonFile[]>(json, _settings);
        }
        
        // Get Addon File
        
        private static readonly ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>> _addonFileTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>>();
        
        public static Task<AddonFile> GetAddonFile(int addonId, int fileId) =>
            _addonFileTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileInternal(id.Item1, id.Item2));
        
        private static async Task<AddonFile> GetAddonFileInternal(int addonId, int fileId) 
        {
            Debug.WriteLine($"getAddonFile { addonId } { fileId }"); // TODO: verbose logging
            var jsonFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/{ fileId }.json", $"addon/{ addonId }/files/{ fileId }.json");
            var json = File.ReadAllText(jsonFile.FullPath);
            return JsonConvert.DeserializeObject<AddonFile>(json, _settings);
        }
        
        // Get Addon File Changelog
        
        private static readonly ConcurrentDictionary<Tuple<int, int>, Task<string>> _addonFileChangelogTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<string>>();

        public static Task<string> GetAddonFileChangelog(int addonId, int fileId) =>
            _addonFileChangelogTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileChangelogInternal(id.Item1, id.Item2));
        
        private static async Task<string> GetAddonFileChangelogInternal(int addonId, int fileId) 
        {
            Debug.WriteLine($"getAddonFileChangelog { addonId } { fileId }"); // TODO: verbose logging
            var htmlFile = await _downloader.Download($"{ URL_BASE }/addon/{ addonId }/files/{ fileId }.changelog.html", $"addon/{ addonId }/files/{ fileId }.changelog.html");
            return File.ReadAllText(htmlFile.FullPath);
        }
    }
}
