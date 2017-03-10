using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.Config
{
    public class ModpackBuild
    {
        public string Name { get; set; }
        public string Description { get; set; }
        [JsonProperty("version")]
        public string PackVersion { get; set; }
        public EntryLinks Links { get; set; }
        
        public List<string> Authors { get; set; }
        public List<string> Contributors { get; set; }
        
        [JsonProperty("mcVersion")]
        public string MinecraftVersion { get; set; }
        
        public EntryDefaults Defaults { get; set; }
        
        public List<EntryMod> Mods { get; set; }
        
        
        public ModpackBuild(ModpackConfig config)
        {
            Name         = config.Name;
            Description  = config.Description;
            PackVersion  = config.PackVersion;
            Links        = config.Links;
            Authors      = config.Authors;
            Contributors = config.Contributors;
        }
        
        
        public void Save(string path, bool pretty = false)
        {
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
