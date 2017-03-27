using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json;

namespace GitMC.Lib.Curse
{
    public class LatestProjects
    {
        private static readonly string complete_url = "http://clientupdate-v6.cursecdn.com/feed/addons/432/v10/complete.json.bz2"; 
        private static readonly string complete_timestamp_url = "http://clientupdate-v6.cursecdn.com/feed/addons/432/v10/complete.json.bz2.txt";
        
        public static async Task<ProjectList> Get()
        {
            String cache = Path.Combine(Constants.CachePath, "curse");
            // Console.WriteLine($"cache: {cache}"); //TODO: verbose
            
            HttpClient client = new HttpClient(); //TODO: use same HttpClient everywhere
            
            var completeFile = Path.Combine(cache, "complete.json");
            var completeFileTimestamp = Path.Combine(cache, "complete.txt");
            // read timestamp
            var timestamp = await client.GetStringAsync(complete_timestamp_url);
            String uncompressedString = null;
            if(File.Exists(completeFileTimestamp) && File.Exists(completeFile))
            {
                var localTimestamp = File.ReadAllText(completeFileTimestamp);
                if(localTimestamp == timestamp)
                {
                    //if complete.json exists, read it
                    uncompressedString = File.ReadAllText(completeFile);
                }
            }
            
            // download and decompress
            if (string.IsNullOrEmpty(uncompressedString)) {
                Console.WriteLine($"downloading complete.json.bz2 and decompressing into {cache}"); //TODO: verbose logging
                using (Stream stream = await client.GetStreamAsync(complete_url))
                {
                    using (MemoryStream target = new MemoryStream())
                    {
                        BZip2.Decompress(stream, target, true);
                        uncompressedString = Encoding.UTF8.GetString(target.ToArray());
                    }
                }
                //save to cache
                Directory.CreateDirectory(Path.GetDirectoryName(completeFile));
                File.WriteAllText(completeFile, uncompressedString);
            }
            
            var settings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Error,
                //NullValueHandling = NullValueHandling.Ignore,
                // ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            var allProjects = JsonConvert.DeserializeObject<ProjectList>(uncompressedString, settings);
            
            Directory.CreateDirectory(Path.GetDirectoryName(completeFileTimestamp));
            File.WriteAllText(completeFileTimestamp, allProjects.timestamp.ToString());
            
            return allProjects;
        }
    }
    
    public class ProjectList
    {
        public long timestamp {get; set;}
        public List<Addon> data {get; set;}
    }
}