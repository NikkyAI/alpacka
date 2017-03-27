using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace GitMC.Lib.Config
{
    public class ModpackVersion
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Contributors { get; set; }
        public EntryLinks Links { get; set; }
        
        [JsonProperty("version"), YamlMember(Alias = "version")]
        public string PackVersion { get; set; }
        [Required, JsonProperty("mcVersion"), YamlMember(Alias = "mcVersion")]
        public string MinecraftVersion { get; set; }
        public string ForgeVersion { get; set; }
        
        [Required]
        public List<EntryMod> Mods { get; set; }
        
        
        public ModpackVersion Clone() =>
            new ModpackVersion {
                Name         = Name,
                Description  = Description,
                Authors      = Authors?.ToList(),
                Contributors = Contributors?.ToList(),
                Links        = Links?.Clone(),
                PackVersion      = PackVersion,
                MinecraftVersion = MinecraftVersion,
                ForgeVersion     = ForgeVersion,
                Mods = Mods.Select(mod => mod.Clone()).ToList()
            };
        
        
        public void SaveJSON(string path, bool pretty = false)
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
