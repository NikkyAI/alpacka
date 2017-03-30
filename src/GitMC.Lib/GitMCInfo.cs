using System.IO;
using Newtonsoft.Json;

namespace GitMC.Lib
{
    public class GitMCInfo
    {
        public void Save(string dir) {
            string text = JsonConvert.SerializeObject(this);
            File.WriteAllText(Path.Combine(dir, Constants.INFO_FILE), text);
        }
        
        public static GitMCInfo Load(string dir) {
            string text = File.ReadAllText(Path.Combine(dir, Constants.INFO_FILE));
            return JsonConvert.DeserializeObject<GitMCInfo>(text);
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
