using System.ComponentModel.DataAnnotations;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Pack.Config
{
    public class ModpackConfig : Modpack
    {
        internal static Serializer Serializer { get; } = new SerializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .WithTypeConverter(new EntryIncludes.TypeConverter())
            .Build();
        internal static IValueSerializer ValueSerializer { get; } = new SerializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .WithTypeConverter(new EntryIncludes.TypeConverter())
            .BuildValueSerializer();
        
        internal static Deserializer Deserializer { get; } = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                                    s => s.InsteadOf<ObjectNodeDeserializer>())
            .WithTypeConverter(new EntryIncludes.TypeConverter())
            .Build();
        
        
        public EntryDefaults Defaults { get; set; } = new EntryDefaults();
        
        [Required]
        public EntryIncludes Includes { get; set; }
        
        
        public static ModpackConfig LoadYAML(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_CONFIG_FILE);
            
            ModpackConfig config;
            using (var reader = new StreamReader(File.OpenRead(path)))
                config = Deserializer.Deserialize<ModpackConfig>(reader);
            
            return config;
        }
        
        
        public void SaveYAML(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, Constants.PACK_CONFIG_FILE);
            
            using (var writer = new StreamWriter(File.OpenWrite(path)))
            {
                Serializer.Serialize(writer, this);
            }
        }
    }
}
