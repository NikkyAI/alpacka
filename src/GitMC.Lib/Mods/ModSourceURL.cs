using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public class ModSourceURL : IModSource
    {
        private static HttpClient _client = new HttpClient();
        
        public bool CanHandle(string source) =>
            (source.StartsWith("http://") || source.StartsWith("https://"));
        
        public Task Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency) =>
            Task.CompletedTask;
        
        public async Task<string> Download(EntryMod mod, Stream destination)
        {
            var response = await _client.GetAsync(mod.Source);
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(destination);
            // Try using suggested file name or getting the it from the request uri.
            return response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
                       ?? GetFilenameFromUri(response.RequestMessage.RequestUri);
        }
        private static string GetFilenameFromUri(Uri uri)
        {
            try {
                var filename = Path.GetFileName(uri.ToString());
                if (!filename.EndsWith(".jar")) return null;
                return filename;
            } catch { return null; }
        }
    }
}
