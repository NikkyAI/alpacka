using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GitMC.Lib.Config;
using Newtonsoft.Json;

namespace GitMC.Lib.Installer
{
    public static class Forge
    {
        // static readonly string FORGE_LEGACY_URL = "http://files.minecraftforge.net/minecraftforge/json";
        static readonly string FORGE_GRADLE_URL = "http://files.minecraftforge.net/maven/net/minecraftforge/forge/json";
        
        private static async Task<ForgeVersions> GetForgeVersions() 
        {
            HttpClient client = new HttpClient(); //TODO: use same HttpClient everywhere
            string versionListJson = await client.GetStringAsync(FORGE_GRADLE_URL); //TODO: cache using If-None-Match and Etag
            
            var settings = new JsonSerializerSettings {
                // Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Error,
                //NullValueHandling = NullValueHandling.Ignore,
                // ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            var forgeVersions = JsonConvert.DeserializeObject<ForgeVersions>(versionListJson, settings);
            return forgeVersions;
        }
        
        public static int GetBuildNumber(string mcversion, DefaultVersion version)
        {
            var forgeVersions = GetForgeVersions().Result;
            
            return forgeVersions.promos[$"{mcversion}-{version.ToString().ToLowerInvariant()}"];
        }
        
        public static string GetUrl(string mcversion, DefaultVersion version)
        {
            int buildNumber = GetBuildNumber(mcversion, version);
            return GetUrl(buildNumber);
        }
        
        public static string GetUrl(int forgeBuildNumber)
        {
            var forgeVersions = GetForgeVersions().Result;
            
            var forgeVersion = forgeVersions.number[forgeBuildNumber];
            
            var file = forgeVersion.files.Find(f => f.Part == "installer");

            string longVersion = forgeVersion.mcversion + "-" + forgeVersion.version;
            if (!string.IsNullOrEmpty(forgeVersion.branch))
            {
                longVersion = longVersion + "-" + forgeVersion.branch;
            }

            string filename = forgeVersions.artifact + "-" + longVersion + "-" + file.Part + "." + file.Extension;

            string url = $"{forgeVersions.webpath}/{longVersion}/{filename}";

            return url;
        }
        
        private class ForgeVersions
        {
            public string adfocus { get; set; }
            public string artifact { get; set; }
            public Dictionary<string, List<int>> branches { get; set; }
            public string homepage { get; set; }
            public Dictionary<string, List<int>> mcversion { get; set; }
            public string name { get; set; }
            public Dictionary<int, ForgeVersion> number { get; set; }
            public Dictionary<string, int> promos { get; set; }
            public string webpath { get; set; }
        }
        
        private class ForgeVersion
        {
            public string branch { get; set; }
            public int build { get; set; }
            public List<ForgeFile> files { get; set; }
            public string mcversion { get; set; }
            public double modified { get; set; }
            public string version { get; set; }
        }
        
        // [JsonArrayAttribute]
        private class ForgeFile : List<string>
        {
            public string Extension => this[0];
            public string Part => this[1];
            public string Hash => this[2];
        }
    }
}