using System.IO;

namespace Alpacka.Lib.Instances.MultiMC
{
    public class MultiMCInstance
    {
        public static readonly string CONFIG_FILE = "instance.cfg";
        
        public MultiMCInstance(string name, string version, string notes = "")
            { Name = name; Version = version; Notes = notes; }
        
        public string Name { get; set; }
        public string Notes { get; set; }
        public string Version { get; set; }
        public string InstanceType { get; set; } = "OneSix";
        
        public void Save(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, CONFIG_FILE);
            File.WriteAllText(path,
                $"name={ Name }\n" +
                $"notes={ Notes }\n" +
                $"IntendedVersion={ Version }\n" +
                $"InstanceType={ InstanceType }\n");
        }
        
        /// <summary> Update IntendedVersion by appending to the config file.
        ///           This is a bit hacky but works, as the last occurance counts. </summary>
        // TODO: Implement loading instance.cfg so we don't need to hacky hacky.
        public static void UpdateVersion(string path, string mcversion, string forgeversion)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, CONFIG_FILE);
            File.AppendAllText(path, $"IntendedVersion={ mcversion }\nForgeVersion={ forgeversion }\n");
        }
    }
}
