using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GitMC.Lib.Net
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
            _tempDir = Path.Combine(Path.GetTempPath(), $"gitmc-{ _rnd.Next() }");
            Directory.CreateDirectory(_tempDir);
        }
        
        ~FileDownloaderURL() =>
            Dispose();
        
        public void Dispose()
        {
            if (_disposed) return;
            Directory.Delete(_tempDir, true);
            GC.SuppressFinalize(this);
            _disposed = true;
        }
        
        
        public async Task<DownloadedFile> Download(string url)
        {
            var response = await _client.GetAsync(url);
            
            int redirects = 0;
            while ((response.StatusCode == HttpStatusCode.Moved) ||
                   (response.StatusCode == HttpStatusCode.Redirect) ||
                   (response.StatusCode == HttpStatusCode.TemporaryRedirect)) {
                if (redirects++ > MAX_REDIRECTS)
                    throw new HttpRequestException($"Too many redirects for URL '{ url }'");
                response = await _client.GetAsync(response.Headers.Location);
            }
            
            response.EnsureSuccessStatusCode();
            
            // Try using suggested file name or getting the it from the request uri.
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? GetFileNameFromUri(response.RequestMessage.RequestUri);
            
            if (fileName == null)
                throw new NoFileNameException(url);
            
            return await _cache.Get(fileName, async () => {
                
                var transform = new MD5Transform();
                var tempPath  = Path.Combine(_tempDir, fileName);
                using (var writeStream = new CryptoStream(File.OpenWrite(tempPath), transform, CryptoStreamMode.Write))
                    await response.Content.CopyToAsync(writeStream);
                
                var md5 = BitConverter.ToString(transform.Hash)
                    .Replace("-", "").ToLowerInvariant();
                
                Console.WriteLine($"Downloaded '{ fileName }'");
                return new DownloadedFile(url, tempPath, fileName, md5);
                
            });
        }
        
        private static string GetFileNameFromUri(Uri uri)
        {
            try {
                var fileName = Path.GetFileName(uri.ToString());
                if (!fileName.EndsWith(".jar")) return null;
                return fileName;
            } catch { return null; }
        }
    }
    
    public class NoFileNameException : Exception
    {
        public string DownloadURL { get; }
        
        public NoFileNameException(string url)
            : base($"Could not get file name from URL '{ url }'")
            { DownloadURL = url; }
    }
}
