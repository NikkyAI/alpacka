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
    public class EntryDefaults : List<EntryDefaults.Group>
    {
        public Group this[string name] =>
            this.FirstOrDefault(group => (group.Name == name));
        
        public EntryDefaults()
        {
            Add(new Group("mods") {
                Path = "mods",
                Handler = "curse",
                Version = Release.Recommended
            });
            Add(new Group("config") {
                Path = "config",
                Handler = "file"
            });
            
            Add(new Group("client") { Side = Side.Client });
            Add(new Group("server") { Side = Side.Server });
            
            Add(new Group("curse") { Handler = "Curse" });
            Add(new Group("github") { Handler = "GitHub" });
            
            Add(new Group("recommended") { Version = Release.Recommended });
            Add(new Group("latest") { Version = Release.Latest });
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
        
        
        public class TypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type) =>
                typeof(EntryDefaults).GetTypeInfo().IsAssignableFrom(type);
            
            public object ReadYaml(IParser parser, Type type)
            {
                var groups = ModpackConfig.Deserializer
                    .Deserialize<Dictionary<string, Group>>(parser);
                var defaults = new EntryDefaults();
                foreach (var pair in groups) {
                    var group  = pair.Value;
                    group.Name = pair.Key;
                    defaults.Add(group);
                }
                return defaults;
            }
            
            public void WriteYaml(IEmitter emitter, object value, Type type)
            {
                emitter.Emit(new MappingStart());
                foreach (var group in (EntryDefaults)value) {
                    emitter.Emit(new Scalar(group.Name));
                    ModpackConfig.Serializer.Serialize(emitter, value);
                }
                emitter.Emit(new MappingEnd());
            }
        }
    }
}
