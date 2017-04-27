using System.Collections.Generic;

namespace Alpacka.Lib.Pack
{
    public class EntryDefaults : Dictionary<string, DefaultsGroup>
    {
        public EntryDefaults()
        {
            Add("mods", new DefaultsGroup {
                Path = "mods",
                Handler = "curse",
                Version = Release.Recommended
            });
            Add("config", new DefaultsGroup {
                Path = "config",
                Handler = "file"
            });
            
            Add("client", new DefaultsGroup { Side = Side.Client });
            Add("server", new DefaultsGroup { Side = Side.Server });
            
            Add("curse", new DefaultsGroup { Handler = "Curse" });
            Add("github", new DefaultsGroup { Handler = "GitHub" });
            
            Add("recommended", new DefaultsGroup { Version = Release.Recommended });
            Add("latest", new DefaultsGroup { Version = Release.Latest });
        }
    }
    
    public class DefaultsGroup
    {
        /// <summary> Name of the ISourceHandler to use for
        ///           contained resources if Source is ambiguous. </summary>
        public string Handler { get; set; }
        
        /// <summary> Default version of contained resources, if any.
        ///           Currently only applies to mods. </summary>
        public Release? Version { get; set; }
        
        /// <summary> Destination (and sometimes relative
        ///           source) path of contained resources. </summary>
        public string Path { get; set; }
        
        /// <summary> Side of contained resources. If not Both,
        ///           they will be only be available on this side. </summary>
        public Side? Side { get; set; }
    }
}
