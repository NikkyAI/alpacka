using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Utility
{
    public static class ExtensionMethods
    {
        private static readonly JsonSerializerSettings _serializerSettings =
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter { CamelCaseText = true } }
            };
        
        public static void ClearReadOnly(this DirectoryInfo directory)
        {
            if (directory == null) return;
            directory.Attributes = FileAttributes.Normal;
            foreach (var file in directory.EnumerateFiles())
                file.Attributes = FileAttributes.Normal;
            foreach (var dir in directory.EnumerateDirectories())
                dir.ClearReadOnly();
        }
        
        public static string ToPrettyJson(this object obj) =>
            JsonConvert.SerializeObject(obj, _serializerSettings);
        
        private static readonly Serializer serializer = 
            new SerializerBuilder()
                .Build();
        
        public static string ToPrettyYaml(this object obj) => serializer.Serialize(obj);
    }
}