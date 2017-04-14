using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Alpacka.Lib.Instances.MultiMC
{
    public static class MultiMCMeta
    {
        private static readonly string BASE_URL = "https://meta.multimc.org/net.minecraftforge";
        
        private static HttpClient _client = new HttpClient();
        
        public static string GetFullVersion(int buildNumber)
        {
            var json = _client.GetStringAsync(BASE_URL).Result;
            
            var settings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var metaForge = JsonConvert.DeserializeObject<MetaForge>(json, settings);
            
            var versions = metaForge.Versions;
            var fullVersion = versions.Find(v => v.Version.Contains($"{ buildNumber }"));
            return fullVersion.Version;
        }
        
        public static string GetForgePatch(string fullVersion)
        {
            var json = _client.GetStringAsync($"{ BASE_URL }/{ fullVersion }.json").Result;
            return json.Replace("\"uid\"", "\"fileId\"");
        }
        
        private class MetaForge
        {
            public int FormatVersion { get; set; }
            public string Name { get; set; }
            public string ParentUid { get; set; }
            public string Uid { get; set; }
            public List<ForgeVersion> Versions { get; set; }
        }
        
        private class ForgeVersion
        {
            public string ReleaseTime { get; set; }
            public Dictionary<string, string> Requires { get; set; }
            public string Sha256 { get; set; }
            public string Version { get; set; }
        }
    }
}