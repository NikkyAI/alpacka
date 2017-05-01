using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Pack.Config
{
    public class EntryDefaults : ICollection<EntryDefaults.Group>
    {
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
        }
        
        
        public class TypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type) =>
                typeof(EntryIncludes).GetTypeInfo().IsAssignableFrom(type);
            
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
            
            // TODO: Implement EntryDefaults.TypeConverter.WriteYaml.
            public void WriteYaml(IEmitter emitter, object value, Type type) =>
                throw new NotImplementedException();
        }
        
        
        // ICollection implementation
        
        private readonly Dictionary<string, Group> _dict =
            new Dictionary<string, Group>();
        
        public int Count => _dict.Count;
        bool ICollection<Group>.IsReadOnly => false;
        
        public void Add(Group group) =>
            _dict.Add(group.Name, group);
        
        public bool Contains(Group group)
        {
            Group found;
            return (_dict.TryGetValue(group.Name, out found) &&
                    (found == group));
        }
        
        public bool Remove(Group group)
        {
            Group found;
            if (!_dict.TryGetValue(group.Name, out found) ||
                (found != group)) return false;
            _dict.Remove(group.Name);
            return true;
        }
        
        public void Clear() => _dict.Clear();
        
        public IEnumerator<Group> GetEnumerator() => _dict.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        void ICollection<Group>.CopyTo(Group[] array, int arrayIndex) =>
            _dict.Values.CopyTo(array, arrayIndex);
    }
}
