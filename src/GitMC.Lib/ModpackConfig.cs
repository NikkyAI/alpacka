using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitMC.Lib
{
    public class ModpackConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Contributers { get; set; }
        
        public static ModpackConfig Load(string path)
        {
            var str = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            return deserializer.Deserialize<ModpackConfig>(str);
        }
    }
}
