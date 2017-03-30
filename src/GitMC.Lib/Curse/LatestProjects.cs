using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.Curse
{
    public class LatestProjects
    {
        private static readonly string COMPLETE_URL = "http://clientupdate-v6.cursecdn.com/feed/addons/432/v10/complete.json.bz2"; 
        private static readonly string COMPLETE_URL_TIMESTAMP = "http://clientupdate-v6.cursecdn.com/feed/addons/432/v10/complete.json.bz2.txt";
        
        public static async Task<ProjectList> Get()
        {
            var cache = Path.Combine(Constants.CachePath, "curse");
            // Console.WriteLine($"cache: { cache }"); // TODO: verbose
            
            var client = new HttpClient(); // TODO: use same HttpClient everywhere
            
            var completeFile = Path.Combine(cache, "complete.json");
            var completeFileTimestamp = Path.Combine(cache, "complete.txt");
            // read timestamp
            var timestamp = await client.GetStringAsync(COMPLETE_URL_TIMESTAMP);
            String uncompressedString = null;
            if (File.Exists(completeFileTimestamp) && File.Exists(completeFile)) {
                var localTimestamp = File.ReadAllText(completeFileTimestamp);
                if (localTimestamp == timestamp) // if complete.json exists, read it
                    uncompressedString = File.ReadAllText(completeFile);
            }
            
            // download and decompress
            if (string.IsNullOrEmpty(uncompressedString)) {
                Console.WriteLine($"downloading complete.json.bz2 and decompressing into { cache }"); // TODO: verbose logging
                using (var stream = await client.GetStreamAsync(COMPLETE_URL))
                using (var target = new MemoryStream()) {
                    BZip2.Decompress(stream, target, true);
                    uncompressedString = Encoding.UTF8.GetString(target.ToArray());
                }
                // save to cache
                Directory.CreateDirectory(Path.GetDirectoryName(completeFile));
                File.WriteAllText(completeFile, uncompressedString);
            }
            
            var settings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Error,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                // NullValueHandling = NullValueHandling.Ignore,
            };
            var allProjects = JsonConvert.DeserializeObject<ProjectList>(uncompressedString, settings);
            
            Directory.CreateDirectory(Path.GetDirectoryName(completeFileTimestamp));
            File.WriteAllText(completeFileTimestamp, allProjects.Timestamp.ToString());
            
            return allProjects;
        }
    }
    
    public class ProjectList
    {
        public long Timestamp { get; set; }
        public List<Addon> Data { get; set; }
    }
}
