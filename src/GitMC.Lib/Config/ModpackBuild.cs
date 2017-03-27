using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.Config
{
    // TODO: Do we need this class? ModpackConfig has the same fields.
    public class ModpackBuild
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Contributors { get; set; }
        public EntryLinks Links { get; set; }
        
        [JsonProperty("version")]
        public string PackVersion { get; set; }
        [JsonProperty("mcVersion")]
        public string MinecraftVersion { get; set; }
        public string ForgeVersion { get; set; }
        
        public List<EntryMod> Mods { get; set; }
        
        
        public ModpackBuild(ModpackConfig config)
        {
            Name         = config.Name;
            Description  = config.Description;
            Authors      = config.Authors;
            Contributors = config.Contributors;
            Links        = config.Links;
            
            PackVersion      = config.PackVersion;
            MinecraftVersion = config.MinecraftVersion;
            ForgeVersion     = config.ForgeVersion;
            
            Mods = config.Mods;
        }
        
        
        public void Save(string path, bool pretty = false)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_BUILD_FILE);
            var settings = new JsonSerializerSettings {
                Formatting = (pretty ? Formatting.Indented : Formatting.None),
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var json = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(path, json);
        }
    }
}
