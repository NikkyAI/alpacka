using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace GitMC.Lib.Config
{
    public class EntryMod
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EntryLinks Links { get; set; }
        
        public string Version { get; set; }
        [YamlMember(Alias = "src")]
        [JsonProperty("mcVersion")]
        public string Source { get; set; }
        public string MD5 { get; set; }
        
        public Side Side { get; set; } = Side.Both;
        public List<string> Feature { get; set; } = new List<string>();
    }
}
