using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.Config
{
    public class ModpackBuild : Modpack
    {
        public string PackVersion { get; set; }
        
        public ModpackBuild(Modpack pack)
        {
            Name = pack.Name;
            Description = pack.Description;
            Authors = pack.Authors;
            Contributors = pack.Contributors;
            Links = pack.Links;
            
            MinecraftVersion = pack.MinecraftVersion;
            ForgeVersion = pack.ForgeVersion;
            
            Mods = pack.Mods;
        }
        
        public void SaveJSON(string path, bool pretty = false)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_BUILD_FILE);
            
            var settings = new JsonSerializerSettings {
                Formatting = (pretty ? Formatting.Indented : Formatting.None),
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter { CamelCaseText = true } }
            };
            
            var json = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(path, json);
        }
    }
}
