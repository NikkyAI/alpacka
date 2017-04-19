using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alpacka.Lib.Config;

namespace Alpacka.Lib.Instances.MultiMC
{
    public class MultiMCHandler : IInstanceHandler
    {
        // FIXME: Use some configuration file instead.
        private static readonly string MULTIMC_PATH = @"C:\D\games\minecraft\MultiMC";
        
        private readonly string _instancesPath;
        
        public string Name => "MultiMC";
        
        public MultiMCHandler()
        {
            if (!Directory.Exists(MULTIMC_PATH)) throw new Exception(
                $"The MultiMC directory does not exist ({ MULTIMC_PATH })");
            // TODO: Verify that the specified path actually contains MultiMC?
            
            _instancesPath = Path.Combine(MULTIMC_PATH, "instances");
        }
        
        public string GetInstancePath(string instanceName) =>
            // TODO: Custom instances folder? Is this possible?
            Path.Combine(_instancesPath, instanceName, "minecraft");
        
        public List<string> GetInstances() =>
            Directory.EnumerateDirectories(_instancesPath)
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
    }
}