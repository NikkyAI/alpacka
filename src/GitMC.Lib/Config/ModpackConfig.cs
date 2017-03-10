using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace GitMC.Lib.Config
{
    public class ModpackConfig
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [YamlMember(Alias = "version")]
        public string PackVersion { get; set; }
        public EntryLinks Links { get; set; }
        
        public List<string> Authors { get; set; }
        public List<string> Contributors { get; set; }
        
        [Required]
        [YamlMember(Alias = "mcVersion")]
        public string MinecraftVersion { get; set; }
        
        public EntryDefaults Defaults { get; set; } = EntryDefaults.Default;
        
        [Required]
        public List<EntryMod> Mods { get; set; }
        
        
        public static ModpackConfig Load(string path)
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                                      s => s.InsteadOf<ObjectNodeDeserializer>())
                .Build();
            using (var reader = new StreamReader(File.OpenRead(path)))
                return deserializer.Deserialize<ModpackConfig>(reader);
        }
    }
}
