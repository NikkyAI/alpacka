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
            
            var instanceConfig = new MultiMCInstance(
                pack.Name, pack.MinecraftVersion, pack.Description);
            instanceConfig.Save(multiMCInstancePath);
            
            // TODO: Add to instance group.
            
            Update(instancePath, null, pack);
        }
        
        public void Update(string instancePath, ModpackBuild oldPack, ModpackBuild newPack)
        {
            var multiMCInstancePath = Path.GetDirectoryName(instancePath);
            
            if (newPack.MinecraftVersion != oldPack?.MinecraftVersion)
                MultiMCInstance.UpdateVersion(multiMCInstancePath, newPack.MinecraftVersion);
            
            if (newPack.ForgeVersion != oldPack?.ForgeVersion) {
                var patchData = MultiMCMeta.GetForgePatch($"{ newPack.MinecraftVersion }-{ newPack.ForgeVersion }");
                var patchPath = Path.Combine(instancePath, "patches", "net.minecraftforge.json");
                Directory.CreateDirectory(Path.GetDirectoryName(patchPath));
                File.WriteAllText(patchPath, patchData);
            }
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
            
            [YamlIgnore]
            public string InstancesPath => Path.Combine(MultiMCPath, "instances");
            
            public static Config Load() => Load<Config>("multimc");
        }
    }
}