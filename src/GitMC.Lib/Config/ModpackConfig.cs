using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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
        
        [Required, YamlMember(Alias = "mcVersion")]
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
            
            using (var reader = new StreamReader(File.OpenRead(path))) {
                var config = deserializer.Deserialize<ModpackConfig>(reader);
                
                foreach (var mod in config.Mods) {
                    // Move @version from source to Version property.
                    {
                        var index = mod.Source.LastIndexOf('@');
                        if (index != -1) {
                            var newSource  = mod.Source.Substring(0, index);
                            var newVersion = mod.Source.Substring(index + 1);
                            if (mod.Version != null)
                                throw new Exception($"Mod '{ mod.Name ?? mod.Source }' has both @version ({ newVersion })" +
                                                    $"and version property ({ mod.Version }) defined.");
                            mod.Source  = newSource;
                            mod.Version = newVersion;
                        }
                    }
                    
                    // TODO: Do this later when mod name is resolved.
                    // Replace "true" feature with mod name.
                    {
                        var index = mod.Feature.IndexOf("true");
                        if (index != -1) mod.Feature[index] = mod.Name;
                    }
                }
                
                return config;
            }
        }
    }
}
