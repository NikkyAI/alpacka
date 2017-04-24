using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using Alpacka.Lib.Config;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Instances.MultiMC
{
    public class MultiMCHandler : IInstanceHandler
    {
        public static readonly string CONFIG_FILE = "instance.cfg";
        private readonly Config _config = Config.Load();
        
        public string Name => "MultiMC";
        
        public MultiMCHandler()
        {
            if (!Directory.Exists(_config.MultiMCPath)) throw new Exception(
                $"MultiMC path does not exist ({ _config.MultiMCPath })");
            // TODO: Verify that the specified path actually contains MultiMC?
        }
        
        public string GetInstancePath(string instanceName) =>
            // TODO: Custom instances folder? Is this possible?
            Path.Combine(_config.InstancesPath, instanceName, "minecraft");
        
        public List<string> GetInstances() =>
            Directory.EnumerateDirectories(_config.InstancesPath)
                .Select(dir => Path.Combine(dir, "minecraft"))
                .Where(Directory.Exists) // FIXME: Only include alpacka instances.
                .ToList();
        
        
        public void Install(string instancePath, ModpackBuild pack)
        {
            var multiMCInstancePath = Path.GetDirectoryName(instancePath);
            var multiMCInstanceCfg = Path.Combine(multiMCInstancePath, CONFIG_FILE);
            
            var cfg = new MultiMCConfig(multiMCInstanceCfg);
            cfg.Config["name"] = pack.Name;
            cfg.Config["notes"] = pack.Description;
            cfg.Config["IntendedVersion"] = pack.MinecraftVersion;
            cfg.Config["InstanceType"] = "OneSix";
            cfg.Save();
            
            // TODO: Add to instance group.
            
            Update(instancePath, null, pack);
        }
        
        public void Update(string instancePath, ModpackBuild oldPack, ModpackBuild newPack)
        {
            var multiMCInstancePath = Path.GetDirectoryName(instancePath);
            var multiMCInstanceCfg = Path.Combine(multiMCInstancePath, CONFIG_FILE);
            
            // update minecraft and forge version
            var cfg = new MultiMCConfig(multiMCInstanceCfg);
            cfg.Config["IntendedVersion"] = newPack.MinecraftVersion;
            cfg.Config["ForgeVersion"] = newPack.ForgeVersion;
            cfg.Save();
        }
        
        public void Remove(string instancePath)
        {
            var multiMCInstancePath = Directory.GetParent(instancePath).FullName;
            Directory.Delete(multiMCInstancePath, true);
        }
        
        
        public class Config : UserConfig
        {
            public override string Name { get; } = "multimc";
            
            public string MultiMCPath { get; set; }
            
            private MultiMCConfig _cfg => new MultiMCConfig(Path.Combine(MultiMCPath, "multimc.cfg"));
            
            [YamlIgnore]
            public string InstancesPath => 
                Path.Combine(MultiMCPath, _cfg.Config["InstanceDir"]);
            
            public static Config Load() => Load<Config>("multimc");
        }
    }
}