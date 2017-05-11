using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alpacka.Lib.Curse;
using Newtonsoft.Json;
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
            Add("mods", new Group() {
                Path = "mods",
                Handler = "curse",
                Version = Release.Recommended.ToString()
            });
            
            Add("config", new Group() {
                Path = "config",
                Handler = "file"
            });
            
            Add("client", new Group() { Side = Side.Client });
            Add("server", new Group() { Side = Side.Server });
            
            Add("curse", new Group() { Handler = "Curse" }); //TODO: get string from resource handler
            Add("github", new Group() { Handler = "GitHub" });
            
            Add("recommended", new Group() { Version = Release.Recommended.ToString() });
            Add("latest", new Group() { Version = Release.Latest.ToString() });
        }
        
        
        public class Group : IEntryResource, IEntryMod
        {
            [YamlIgnore]
            public string GroupName { get; set; }
            
            public Group() {  }
            public Group(string name) { Name = name; }
            
            public string Handler { get; set; }
            
            [YamlMember(Alias = "src"), JsonProperty("src")]
            public string Source { get; set; }
            public string MD5 { get; set; }
            public string Version { get; set; }
            public string Path { get; set; }
            public Side? Side { get; set; }
            
            public string Name { get; set; }
            public string Description { get; set; }
            public EntryLinks Links { get; set; }

            public static Group operator +(Group left, Group right) => new Group {
                Handler     = right?.Handler ?? left?.Handler,
                Version     = right?.Version ?? left?.Version,
                Path        = right?.Path ?? left?.Path,
                Side        = right?.Side ?? left?.Side,
                Name        = right?.Name ?? left?.Name,
                Description = right?.Description ?? left?.Description,
                Links       = right?.Links ?? left?.Links
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
