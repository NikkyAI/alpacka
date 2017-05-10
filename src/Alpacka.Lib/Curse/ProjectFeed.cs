using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Alpacka.Lib.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Alpacka.Lib.Curse
{
    public class ProjectFeed
    {
        private static readonly string COMPLETE_URL = "https://cursemeta.nikky.moe/complete.json"; 
        // private static readonly string COMPLETE_URL = "https://cursemeta.nikky.moe/complete.json.bz2"; 
        
        public static async Task<ProjectList> Get()
        {
            var cache = Path.Combine(Constants.CachePath, "curse");
            Debug.WriteLine($"cache: { cache }");
            
            using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "cursefeed")))
            using (var downloader = new FileDownloader(fileCache))
            {
                Console.WriteLine("Downloading cursemeta project data. This could take a while.");
                var feed = await downloader.Download(COMPLETE_URL);
                
                var completeFile = Path.Combine(cache, "complete.json");
                
                string uncompressedString = File.ReadAllText(feed.FullPath);
                
                //FIXME: use bz2 url and caching together
                // using (var filestream = File.OpenRead(feed.Path))
                // using (var target = new MemoryStream()) {
                //     BZip2.Decompress(filestream, target, true);
                //     uncompressedString = Encoding.UTF8.GetString(target.ToArray());
                // }
                // Directory.CreateDirectory(Path.GetDirectoryName(completeFile));
                // File.WriteAllText(completeFile, uncompressedString);
                
                var settings = new JsonSerializerSettings {
                    Formatting = Formatting.Indented,
                    MissingMemberHandling = MissingMemberHandling.Error,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                };
                var allProjects = JsonConvert.DeserializeObject<ProjectList>(uncompressedString, settings);
                
                return allProjects;
            }
        }
    }
}
