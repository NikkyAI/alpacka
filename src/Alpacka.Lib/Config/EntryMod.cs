using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Config
{
    public class EntryMod
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EntryLinks Links { get; set; }
        
        public string Version { get; set; }
        [Required, YamlMember(Alias = "src"), JsonProperty("src")]
        public string Source { get; set; }
        public string MD5 { get; set; }
        
        public Side Side { get; set; } = Side.Both;
        public EntryFeature Feature { get; set; } = new EntryFeature();
        
        public EntryMod Clone() =>
            new EntryMod {
                Name        = Name,
                Description = Description,
                Links       = Links?.Clone(),
                Version = Version,
                Source  = Source,
                MD5     = MD5,
                Side    = Side,
                Feature = Feature?.Clone()
            };
        
        public static implicit operator EntryMod(string value) =>
            new EntryMod { Source = value };
        
        
        public class EntryFeature : List<string> {
            
            public EntryFeature Clone()
            {
                var clone = new EntryFeature();
                clone.AddRange(this);
                return clone;
            }
            
            public static implicit operator EntryFeature(string value) =>
                new EntryFeature { value };
            
        }
    }
}
