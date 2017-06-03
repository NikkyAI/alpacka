namespace Alpacka.Lib.Curse
{
    public class PackManifest
    {
        public MCEntry Minecraft { get; set; }
        public ManifestType ManifestType { get; set; }
        public int ManifestVersion { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public int ProjectID { get; set; }
        public string Overrides { get; set; }
        public PackFile[] Files { get; set; }
    }
    
    public class PackFile
    {
        public int ProjectID { get; set; }
        public int FileID { get; set; }
        public bool Required { get; set; }
    }
    
    public class MCEntry
    {
        public string Version { get; set; }
        public ModLoader[] ModLoaders { get; set; }
    }
    
    public class ModLoader
    {
        public string Id { get; set; }
        public bool primary { get; set; }
    }
    
    public enum ManifestType
    {
        MinecraftModPack
    }
}