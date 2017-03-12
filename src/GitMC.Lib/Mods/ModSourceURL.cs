using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public class ModSourceURL : IModSource
    {
        private static HttpClient _client = new HttpClient();
        
        public bool CanHandle(string scheme) =>
            ((scheme == "http") || (scheme == "https"));
        
        public Task Resolve(EntryMod mod, string mcVersion, IDependencyHandler dependencies) =>
            Task.CompletedTask;
        
        public async Task Download(EntryMod mod, Stream destination)
        {
            using (var source = await _client.GetStreamAsync(mod.Source))
                await source.CopyToAsync(destination);
        }
    }
}
