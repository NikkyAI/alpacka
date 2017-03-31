using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace GitMC.Lib.Config
{
    public class ModpackConfig : Modpack
    {
        public EntryDefaults Defaults { get; set; } = EntryDefaults.Default;
        
        public static ModpackConfig LoadYAML(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_CONFIG_FILE);
            
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                                      s => s.InsteadOf<ObjectNodeDeserializer>())
                .Build();
            
            ModpackConfig config;
            using (var reader = new StreamReader(File.OpenRead(path)))
                config = deserializer.Deserialize<ModpackConfig>(reader);
            
            foreach (var mod in config.Mods) {
                ExtractSourceVersion(mod);
                ReplaceAutoFeature(mod); // TODO: Do this later when mod name is resolved.
            }
            
            return config;
        }
        
        /// <summary> Extract @version information from mod.Source to mod.Version, if present. </summary>
        private static void ExtractSourceVersion(EntryMod mod)
        {
            var index = mod.Source.LastIndexOf('@');
            if (index == -1) return;
            
            var newSource  = mod.Source.Substring(0, index).Trim();
            var newVersion = mod.Source.Substring(index + 1).Trim();
            
            if (mod.Version != null)
                throw new Exception($"Mod '{ mod.Name ?? mod.Source }' has both @version ({ newVersion })" +
                                    $"and version property ({ mod.Version }) defined.");
            
            mod.Source  = newSource;
            mod.Version = newVersion;
        }
        
        /// <summary> Replaces a feature with the exact name of "true" with mod.Name, if present.
        ///           Allows specifying "feature: true" to automatically create a feature with the mod's name. </summary>
        private static void ReplaceAutoFeature(EntryMod mod)
        {
            var index = mod.Feature.IndexOf("true");
            if (index != -1) mod.Feature[index] = mod.Name;
        }
    }
}
