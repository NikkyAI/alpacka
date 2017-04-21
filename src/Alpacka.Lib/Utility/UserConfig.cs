using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Alpacka.Lib.Utility
{
    public abstract class UserConfig
    {
        private static readonly Serializer _serializer = new SerializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention()).Build();
        
        private static readonly Deserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention()).Build();
        
        [YamlIgnore]
        public abstract string Name { get; }
        
        protected static T Load<T>(string name) where T : UserConfig
        {
            var path = Path.Combine(Constants.ConfigPath, $"{ name }.yaml");
            using (var reader = new StreamReader(File.OpenRead(path)))
                return _deserializer.Deserialize<T>(reader);
        }
        
        public void Save()
        {
            var path = Path.Combine(Constants.ConfigPath, $"{ Name }.yaml");
            using (var writer = new StreamWriter(File.OpenWrite(path)))
                _serializer.Serialize(writer, this);
        }
    }
}
