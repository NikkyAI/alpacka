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
        [YamlMember(Alias = "src"), JsonProperty("src")]
        public string Source { get; set; }
        public string MD5 { get; set; }
        
        public Side Side { get; set; } = Side.Both;
        public EntryFeature Feature { get; set; } = new EntryFeature();
        
        public static implicit operator EntryMod(string value) =>
            new EntryMod { Source = value };
        
        [YamlIgnore, JsonIgnore]
        public string Scheme { get {
            var index = Source.IndexOf(':');
            return (index >= 0) ? Source.Substring(0, index) : null;
        } }
        
        
        public class EntryFeature : List<string> {
            
            public static implicit operator EntryFeature(string value) =>
                new EntryFeature { value };
            
        }
    }
}
