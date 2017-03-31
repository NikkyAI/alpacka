using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.MultiMC
{
    public static class Meta
    {
        private static HttpClient client = new HttpClient();
        
        public static string GetFullVersion(int buildNumber)
        {
            var json = client.GetStringAsync("https://meta.multimc.org/net.minecraftforge").Result;
            
            var settings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var metaForge = JsonConvert.DeserializeObject<MetaForge>(json, settings);
            
            var versions = metaForge.Versions;
            var fullVersion = versions.Find(v => v.Version.Contains($"{buildNumber}"));
            return fullVersion.Version;
        }
        
        public static string GetForgePatch(string fullVersion)
        {
            var json = client.GetStringAsync($"https://meta.multimc.org/net.minecraftforge/{fullVersion}.json").Result;
            
            return json.Replace("\"uid\"", "\"fileId\"");
        }
    }
    
    public class MetaForge {
        public int FormatVersion { get; set; }
        public string Name { get; set; }
        public string ParentUid { get; set; }
        public string Uid { get; set; }
        public List<ForgeVersion> Versions { get; set; }

        public class ForgeVersion
        {
            public string ReleaseTime { get; set; }
            public Dictionary<string, string> Requires { get; set; }
            public string Sha256 { get; set; }
            public string Version { get; set; }
        }
    }
}