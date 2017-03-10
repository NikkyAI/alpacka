using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

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
        
        
        public void Save(string path)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
