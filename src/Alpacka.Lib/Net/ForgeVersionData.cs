using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Net
{
    public class ForgeVersionData
    {
        // private static readonly string FORGE_LEGACY_URL = "http://files.minecraftforge.net/minecraftforge/json";
        private static readonly string FORGE_GRADLE_URL = "http://files.minecraftforge.net/maven/net/minecraftforge/forge/json";
        
        [JsonProperty("adfocus")]
        public string AdFocus { get; set; }
        public string Artifact { get; set; }
        public Dictionary<string, List<int>> Branches { get; set; }
        public string Homepage { get; set; }
        [JsonProperty("mcversion")]
        public Dictionary<string, List<int>> MinecraftVersion { get; set; }
        public string Name { get; set; }
        [JsonProperty("number")]
        public Dictionary<int, ForgeVersion> BuildVersions { get; set; }
        [JsonProperty("promos")]
        public Dictionary<string, int> Promotions { get; set; }
        [JsonProperty("webpath")]
        public string WebPath { get; set; }
        
        
        public ForgeVersion this[int buildNumber] { get {
            ForgeVersion version;
            return BuildVersions.TryGetValue(buildNumber, out version)
                ? version : null;
        } }
        
        public ForgeVersion this[string version] { get {
            int buildNumber;
            if(int.TryParse(version.Split('.').Last(), out buildNumber))
                return BuildVersions[buildNumber];
            throw new ArgumentException("version does not contain a valid buildnumber"); // TODO: Implement this!
        } }
        
        
        public static async Task<ForgeVersionData> Download()
        {
            string versionListJson;
            using (var client = new HttpClient()) // TODO: Use same HttpClient everywhere?
                versionListJson = await client.GetStringAsync(FORGE_GRADLE_URL); // TODO: Cache using If-None-Match and ETag.
            
            var settings = new JsonSerializerSettings {
                MissingMemberHandling = MissingMemberHandling.Error,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.DeserializeObject<ForgeVersionData>(versionListJson, settings);
        }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var version in BuildVersions.Values) {
                version.Data = this;
                foreach (var artifact in version.Artifacts)
                    artifact.Version = version;
            }
        }
        
        public string GetRecentMCVersion(Release releaseType) =>
            Promotions.Keys
                .Where(v => v.EndsWith($"-{ releaseType.ToString().ToLowerInvariant() }"))
                .Select(v => v.Substring(0, v.LastIndexOf('-')))
                .OrderByDescending(v => new Version(v)).First();
        
        public ForgeVersion GetRecent(string mcVersion, Release releaseType)
        {
            var promoStr = $"{ mcVersion }-{ releaseType.ToString().ToLowerInvariant() }";
            int buildNumber;
            var forgeVersion = Promotions.TryGetValue(promoStr, out buildNumber)
                ? this[(int)buildNumber] : null;
            if(forgeVersion == null) {
                forgeVersion = BuildVersions.Where(b => b.Value.MinecraftVersion == mcVersion).OrderBy(b => -b.Key).Select(b => b.Value).FirstOrDefault();
            }
            return forgeVersion;
        }
    }
    
    public class ForgeVersion
    {
        [JsonIgnore]
        public ForgeVersionData Data { get; internal set; }
        
        public string Branch { get; set; }
        public int Build { get; set; }
        [JsonProperty("files")]
        public List<ForgeArtifact> Artifacts { get; set; }
        [JsonProperty("mcversion")]
        public string MinecraftVersion { get; set; }
        public double Modified { get; set; }
        public string Version { get; set; }
        
        public ForgeArtifact GetInstaller() => Artifacts.First(
            artifact => (artifact.Type == "installer"));
        
        public string GetFullVersion(bool includeMCVersion = false)
        {
            var version = Version;
            if (Branch != null) version = $"{ version }-{ Branch }";
            if (includeMCVersion) version = $"{ MinecraftVersion }-{ version }";
            return version;
        }
    }
    
    [JsonConverter(typeof(ForgeFileConverter))]
    public class ForgeArtifact
    {
        public ForgeVersion Version { get; internal set; }
        public ForgeVersionData Data => Version.Data;
        
        public string Extension { get; set; }
        public string Type { get; set; }
        public string Hash { get; set; }
        
        public string GetFullName() => $"{ Data.Artifact }-{ Version.GetFullVersion(true) }-{ Type }.{ Extension }";
        public string GetURL() => $"{ Data.WebPath }/{ Version.GetFullVersion(true) }/{ GetFullName() }";
    }
    
    internal class ForgeFileConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            typeof(ForgeArtifact).GetTypeInfo().IsAssignableFrom(objectType);
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            return new ForgeArtifact {
                Extension = (string)array[0],
                Type = (string)array[1],
                Hash = (string)array[2]
            };
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}
