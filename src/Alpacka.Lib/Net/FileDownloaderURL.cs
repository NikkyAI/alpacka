using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Alpacka.Lib.Net
{
    public class FileDownloaderURL : IFileDownloader, IDisposable
    {
        private static readonly int MAX_REDIRECTS = 8;
        
        private static readonly Random _rnd = new Random();
        private static HttpClient _client = new HttpClient();
        
        private readonly FileCache _cache;
        private readonly string _tempDir;
        private bool _disposed = false;
        
        public FileDownloaderURL(FileCache cache)
        {
            _cache = cache;
            _tempDir = Path.Combine(Path.GetTempPath(), $"alpacka-{ _rnd.Next() }");
            Directory.CreateDirectory(_tempDir);
        }
        
        ~FileDownloaderURL() =>
            Dispose();
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Directory.Delete(_tempDir, true);
            GC.SuppressFinalize(this);
        }
        
        
        public Task<DownloadedFile> Download(string url, string relativePath = null) =>
            _cache.Get(url, async oldFile => {
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (oldFile?.ETag != null) request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(oldFile.ETag));
                else if (oldFile?.LastModified != null) request.Headers.IfModifiedSince = oldFile.LastModified;
                
                var response = await _client.SendAsync(request);
                
                // Follow redirects. For some reason this isn't done automatically?
                int redirects = 0;
                while ((response.StatusCode == HttpStatusCode.Moved) ||
                    (response.StatusCode == HttpStatusCode.Redirect) ||
                    (response.StatusCode == HttpStatusCode.TemporaryRedirect)) {
                    if (redirects++ > MAX_REDIRECTS)
                        throw new HttpRequestException($"Too many redirects for URL '{ url }'");
                    response = await _client.GetAsync(response.Headers.Location);
                }
                
                // If file was not modified, return the old one.
                if (response.StatusCode == HttpStatusCode.NotModified) {
                    Debug.WriteLine($"Got '{ oldFile }' from cache");
                    return oldFile;
                }
                
                response.EnsureSuccessStatusCode();
                
                relativePath = relativePath
                    ?? response.Content.Headers.ContentDisposition?.FileNameStar?.Trim('"') // Might be wrapped in quotes which
                    ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')     // are not stripped automatically.
                    ?? GetFileNameFromURI(response.RequestMessage.RequestUri);
                if (relativePath == null) throw new NoFileNameException(url);
                
                var transform = new MD5Transform();
                var tempPath  = Path.Combine(_tempDir, GetRandomFileName());
                using (var writeStream = new CryptoStream(File.OpenWrite(tempPath), transform, CryptoStreamMode.Write))
                    await response.Content.CopyToAsync(writeStream);
                
                var file = new DownloadedFile(url, tempPath, relativePath,
                    lastModified: response.Content.Headers.LastModified,
                    eTag: response.Headers.ETag?.Tag,
                    md5: BitConverter.ToString(transform.Hash)
                        .Replace("-", "").ToLowerInvariant());
                Debug.WriteLine($"Downloaded '{ file }'");
                return file;
                
            });
        
        private static string GetFileNameFromURI(Uri uri)
        {
            try {
                var fileName = Path.GetFileName(uri.ToString());
                if (fileName.IndexOf('.') < 0) return null;
                return fileName;
            } catch { return null; }
        }
        
        private static string GetRandomFileName() =>
            Guid.NewGuid().ToString();
    }
    
    public class NoFileNameException : Exception
    {
        public string DownloadURL { get; }
        
        public NoFileNameException(string url)
            : base($"Could not get file name from URL '{ url }'")
            { DownloadURL = url; }
    }
}
