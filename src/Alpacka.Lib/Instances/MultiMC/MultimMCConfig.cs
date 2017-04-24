using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alpacka.Lib.Instances.MultiMC
{
    public class MultiMCConfig
    {
        public string ConfigFile { get; private set; }
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();
        
        public MultiMCConfig(string file)
        {
            ConfigFile = file;
            if(File.Exists(ConfigFile))
                Load();
        }
        
        public Dictionary<string, string> Load(string file)
        {
            ConfigFile = file;
            return Load();
        }
        
        public Dictionary<string, string> Load()
        {
            var input = File.ReadAllLines(ConfigFile);
            Config = input.Select(value => value.Split(new char[]{'='}, 2))
                .ToDictionary(pair => pair[0], pair => pair[1]);
            return Config;
        }
        
        public void Save(string file)
        {
            ConfigFile = file;
            Save();
        }
        
        public void Save()
        {
            Directory.GetParent(ConfigFile).Create();
            File.WriteAllText(ConfigFile, Config.ToValuePairs());
        }
    }
    
    public static class MultiMCConfigExtension
    {
        public static string ToValuePairs(this Dictionary<string, string> cfg)
        {
            return string.Join("\n", cfg.Select(x => x.Key + "=" + x.Value));
        }
    }
}
