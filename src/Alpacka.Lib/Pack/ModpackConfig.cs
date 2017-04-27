using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Pack
{
    public class ModpackConfig : Modpack
    {
        public EntryDefaults Defaults { get; set; }
        
        [Required]
        public Entries Includes { get; set; }
        
        public static ModpackConfig LoadYAML(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_CONFIG_FILE);
            
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                                      s => s.InsteadOf<ObjectNodeDeserializer>())
                .Build();
            
            ModpackConfig config;
            using (var reader = new StreamReader(File.OpenRead(path)))
                config = deserializer.Deserialize<ModpackConfig>(reader);
            
            return config;
        }
        
        
        public interface IEntry {  }
        
        public class Entries : Dictionary<string, IEntry>, IEntry {  }
        
        public class Resources : List<EntryResource>, IEntry {  }
        
        public class Feature : Entries
        {
            [YamlMember(Alias = "feature")]
            public string Type { get; set; }
        }
    }
}
