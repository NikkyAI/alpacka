using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Alpacka.Lib.Pack
{
    /// <summary> Represents a alpacka modpack. Base class
    ///           for ModpackConfig and ModpackBuild. </summary>
    public abstract class Modpack
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Contributors { get; set; }
        public EntryLinks Links { get; set; }
        
        [Required]
        public string MinecraftVersion { get; set; }
        public string ForgeVersion { get; set; }
    }
}
