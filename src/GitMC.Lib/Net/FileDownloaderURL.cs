using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GitMC.Lib.Net
{
    public class FileDownloaderURL : IFileDownloader
    {
        private static readonly int MAX_REDIRECTS = 8;
        private static HttpClient _client = new HttpClient();
        
        public async Task<DownloadedFile> Download(string url)
        {
            var tempPath = Path.GetTempFileName();
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
            
            var transform = new MD5Transform();
            using (var writeStream = new CryptoStream(File.OpenWrite(tempPath), transform, CryptoStreamMode.Write))
                await response.Content.CopyToAsync(writeStream);
            
            var md5 = BitConverter.ToString(transform.Hash)
                .Replace("-", "").ToLowerInvariant();
            
            return new DownloadedFile(url, tempPath, fileName, md5);
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
}
