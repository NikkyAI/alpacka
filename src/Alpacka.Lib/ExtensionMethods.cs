using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Alpacka.Lib
{
    public static class ExtensionMethods
    {
        public static void ClearReadOnly(this DirectoryInfo parentDirectory)
        {
            if(parentDirectory != null)
            {
                parentDirectory.Attributes = FileAttributes.Normal;
                foreach (FileInfo fi in parentDirectory.GetFiles())
                {
                    fi.Attributes = FileAttributes.Normal;
                }
                foreach (DirectoryInfo di in parentDirectory.GetDirectories())
                {
                    di.ClearReadOnly();
                }
            }
        }
        
        private static readonly JsonSerializerSettings settings =
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter { CamelCaseText = true } }
            };
        
        public static string ToPrettyJson(this object obj) => JsonConvert.SerializeObject(obj, settings);
    }
}