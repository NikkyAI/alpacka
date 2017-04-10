using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib
{
    public class GitMCInfo
    {
        static JsonSerializerSettings settings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter { CamelCaseText = true } }
            };
        public void Save(string dir) {
            string text = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(Path.Combine(dir, Constants.INFO_FILE), text);
        }
        
        public static GitMCInfo Load(string dir) {
            string text = File.ReadAllText(Path.Combine(dir, Constants.INFO_FILE));
            return JsonConvert.DeserializeObject<GitMCInfo>(text, settings);
        }
        public InstallType Type { get; set; }
    }
    
    public enum InstallType
    {
        Vanilla,
        Server,
        MultiMC
    }
}
