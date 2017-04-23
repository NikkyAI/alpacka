using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Alpacka.Lib.Net
{
    public class FileCache : IDisposable
    {
        private static readonly string CACHE_LIST_FILE = "cache.json";
        
        private static readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        
        
        private readonly Dictionary<string, Task<DownloadedFile>> _dict =
            new Dictionary<string, Task<DownloadedFile>>();
        private bool _disposed = false;
        
        public string CacheDirectory { get; }
        public string CacheListPath { get; }
        
        
        public FileCache(string directory)
        {
            Directory.CreateDirectory(directory);
            CacheDirectory = directory;
            CacheListPath  = Path.Combine(CacheDirectory, CACHE_LIST_FILE);
            
            if (!File.Exists(CacheListPath)) return;
            var contents = File.ReadAllText(CacheListPath);
            var files = JsonConvert.DeserializeObject<DownloadedFile[]>(contents, _jsonSettings);
            foreach (var file in files) {
                file.FullPath = Path.Combine(directory, file.RelativePath);
                if (!File.Exists(file.FullPath)) continue;
                _dict.Add(file.URL, Task.FromResult(file));
            }
        }
        
        ~FileCache() => Dispose();
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var files = _dict.Values
                .Where(task => task.IsCompleted)
                .Select(task => task.Result)
                .ToArray();
            var str = JsonConvert.SerializeObject(files, _jsonSettings);
            File.WriteAllText(CacheListPath, str);
            GC.SuppressFinalize(this);
        }
        
        
        public Task<DownloadedFile> Get(string url, Func<DownloadedFile, Task<DownloadedFile>> factory)
        { lock (_dict) {
            Task<DownloadedFile> oldTask;
            // Check if there's already a task in the dictionary
            // for this URL. (Skip faulted and cancelled tasks.)
            return _dict[url] = (_dict.TryGetValue(url, out oldTask) &&
                                 !oldTask.IsFaulted && !oldTask.IsCanceled)
                // If so, check if the task is either not completed or still recent.
                ? (!oldTask.IsCompleted || IsTaskRecent(oldTask))
                    ? oldTask // Return task if recent, or call factory with old file to see if it's ..
                    : ProcessTask(oldTask.Result, factory) // .. still valid or download the new one.
                // Otherwise create a new download task for this URL.
                : ProcessTask(null, factory);
        } }
        
        private Task<DownloadedFile> ProcessTask(DownloadedFile oldFile, Func<DownloadedFile, Task<DownloadedFile>> factory) =>
            factory(oldFile).ContinueWith((task, state) => {
                // If the result is not the same as the old downloaded
                // file (may be null), move it into the cache directory.
                if (task.Result != oldFile) {
                    if (oldFile != null) File.Delete(oldFile.FullPath); // Delete the old one!
                    var destination = Path.Combine(CacheDirectory, task.Result.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    task.Result.Move(destination);
                }
                return task.Result;
            }, DateTime.Now); // Set the tasks AsyncState to the current time.
        
        private bool IsTaskRecent(Task task) =>
            (DateTime.Now - (DateTime?)task.AsyncState) < TimeSpan.FromSeconds(10);
    }
}
