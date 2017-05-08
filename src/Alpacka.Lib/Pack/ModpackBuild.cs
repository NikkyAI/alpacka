using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Alpacka.Lib.Pack
{
    public class ModpackBuild : Modpack
    {
        public string PackVersion { get; set; }
        
        public List<EntryMod> Mods { get; set; }
        
        public ModpackBuild() {  }
        
        public static ModpackBuild CopyFrom(Modpack pack)
        {
            var copy = new ModpackBuild();
            
            List<T> CopyList<T>(List<T> list) =>
                (list != null) ? new List<T>(list) : null;
            
            copy.Name         = pack.Name;
            copy.Description  = pack.Description;
            copy.Authors      = CopyList(pack.Authors);
            copy.Contributors = CopyList(pack.Contributors);
            copy.Links        = pack.Links?.Clone();
            
            copy.MinecraftVersion = pack.MinecraftVersion;
            copy.ForgeVersion     = pack.ForgeVersion;
            
            return copy;
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
