using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Pack.Config
{
    public class EntryDefaults : Dictionary<string, EntryDefaults.Group>
    {        
        new public void Add(string name, Group group) {
            group.Name = name;
            base.Add(name, group);
        }
        
        public EntryDefaults()
        {
            Add("mods", new Group() {
                Path = "mods",
                Handler = "curse",
                Version = Release.Recommended
            });
            
            Add("config", new Group() {
                Path = "config",
                Handler = "file"
            });
            
            Add("client", new Group() { Side = Side.Client });
            Add("server", new Group() { Side = Side.Server });
            
            Add("curse", new Group() { Handler = "Curse" }); //TODO: get string from resource handler
            Add("github", new Group() { Handler = "GitHub" });
            
            Add("recommended", new Group() { Version = Release.Recommended });
            Add("latest", new Group() { Version = Release.Latest });
        }
        
        
        public class Group
        {
            [YamlIgnore]
            public string Name { get; set; }
            
            public Group() {  }
            public Group(string name) { Name = name; }
            
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
            
            
            public static Group operator +(Group left, Group right) => new Group {
                Handler = right?.Handler ?? left?.Handler,
                Version = right?.Version ?? left?.Version,
                Path    = right?.Path ?? left?.Path,
                Side    = right?.Side ?? left?.Side
            };
        }
    }
}
