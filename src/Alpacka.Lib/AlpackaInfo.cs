using System.IO;
using Newtonsoft.Json;

namespace Alpacka.Lib
{
    public class AlpackaInfo
    {
        public void Save(string dir) {
            string text = JsonConvert.SerializeObject(this);
            File.WriteAllText(Path.Combine(dir, Constants.INFO_FILE), text);
        }
        
        public static AlpackaInfo Load(string dir) {
            string text = File.ReadAllText(Path.Combine(dir, Constants.INFO_FILE));
            return JsonConvert.DeserializeObject<AlpackaInfo>(text);
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
