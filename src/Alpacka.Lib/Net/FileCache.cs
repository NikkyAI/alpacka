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
            
            if (File.Exists(CacheListPath)) {
                var files = JsonConvert.DeserializeObject<DownloadedFile[]>(
                    File.ReadAllText(CacheListPath));
                foreach (var file in files) {
                    file.Path = Path.Combine(directory, file.FileName);
                    if (!File.Exists(file.Path)) continue;
                    _dict.Add(file.FileName, Task.FromResult(file));
                }
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
            var str = JsonConvert.SerializeObject(files, Formatting.Indented);
            File.WriteAllText(CacheListPath, str);
            GC.SuppressFinalize(this);
        }
        
        
        public Task<DownloadedFile> Get(string name, Func<Task<DownloadedFile>> factory)
        { lock (_dict) {
            Task<DownloadedFile> oldTask;
            return (!_dict.TryGetValue(name, out oldTask) ||
                    oldTask.IsCanceled || oldTask.IsFaulted)
                ? _dict[name] = Move(name, factory) : oldTask;
        } }
        
        public Task<DownloadedFile> Replace(string name, Func<Task<DownloadedFile>> factory)
            { lock (_dict) return _dict[name] = Move(name, factory); }
        
        
        /// <summary> Moves the DownloadedFile to the cache Directory after completion. </summary>
        private Task<DownloadedFile> Move(string name, Func<Task<DownloadedFile>> factory) =>
            factory().ContinueWith(task => task.Result.Move(Path.Combine(CacheDirectory, name), true));
    }
}
