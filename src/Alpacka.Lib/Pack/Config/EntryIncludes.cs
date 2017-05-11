using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Pack.Config
{
    public class EntryIncludes : List<EntryIncludes.Group>
    {
        public class Group : List<object>
        {
            public string FullName { get; }
            public List<string> Names { get; }
            
            public Group(string fullName)
            {
                FullName = fullName;
                Names = !string.IsNullOrEmpty(fullName)
                    ? fullName.Split('&').Select(name => name.Trim()).ToList()
                    : new List<string>();
            }
        }
        
        public class Feature : Group
        {
            public string Type { get; }
            public string Name { get; }
            
            private static int ampersandIndex;
            public Feature(string type, string fullName)
                : base(SkipFirstName(fullName))
            {
                Type = type;
                Name = fullName.Substring(0, ampersandIndex).TrimEnd();
            }
            
            private static string SkipFirstName(string fullName) =>
                ((ampersandIndex = fullName?.IndexOf('&') ?? -1) >= 0)
                    ? fullName.Substring(ampersandIndex + 1).TrimStart()
                    : null;
        }
        
        
        public class TypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type) =>
                typeof(EntryIncludes).GetTypeInfo().IsAssignableFrom(type);
            
            public object ReadYaml(IParser parser, Type type)
            {
                var entries = new EntryIncludes();
                parser.Expect<MappingStart>();
                while (parser.Current is Scalar) {
                    var fullName = parser.Expect<Scalar>().Value;
                    entries.Add(ReadGroup(parser, fullName));
                }
                parser.Expect<MappingEnd>();
                return entries;
            }
            
            public Group ReadGroup(IParser parser, string fullName)
            {
                Group group;
                if (parser.Current is MappingStart) {
                    parser.Expect<MappingStart>();
                    // If the Group starts with a property called "feature", create a Feature.
                    if (parser.Peek<Scalar>()?.Value == "feature") {
                        parser.Expect<Scalar>(); // Read and drop "feature"
                        var type = parser.Expect<Scalar>().Value;
                        group = new Feature(type, fullName);
                    // Otherwise just create a basic Group.
                    } else group = new Group(fullName);
                    // Read this Group's subgroups.
                    while (parser.Current is Scalar) {
                        var subGroupFullName = parser.Expect<Scalar>().Value;
                        group.Add(ReadGroup(parser, subGroupFullName));
                    }
                    parser.Expect<MappingEnd>();
                } else if (parser.Current is SequenceStart) {
                    var list = ModpackConfig.Deserializer
                        .Deserialize<List<EntryResource>>(parser);
                    group = new Group(fullName);
                    group.AddRange(list);
                } else throw new YamlException(parser.Current.Start, parser.Current.End,
                    $"Expected 'MappingStart' or 'SequenceStart', got '{ parser.Current.GetType().Name }' (at { parser.Current.Start }).");
                return group;
            }
            
            public void WriteYaml(IEmitter emitter, object value, Type type)
            {
                emitter.Emit(new MappingStart());
                foreach (var group in (EntryIncludes)value)
                    WriteGroup(emitter, group);
                emitter.Emit(new MappingEnd());
            }
            
            public void WriteGroup(IEmitter emitter, Group group)
            {
                // Emit the full group name (i.e. "foo & bar & baz").
                emitter.Emit(new Scalar(group.FullName));
                
                // If the group is a feature, make sure the feature name is included.
                var feature = (group as Feature);
                if (feature != null) {
                    emitter.Emit(new Scalar(feature.FullName));
                    emitter.Emit(new Scalar(feature.Name));
                }
                
                // If this group contains subgroups, emit these in a map.
                if (group.FirstOrDefault() is Group) {
                    emitter.Emit(new MappingStart());
                    foreach (var subGroup in group.Cast<Group>())
                        WriteGroup(emitter, subGroup);
                    emitter.Emit(new MappingEnd());
                // Otherwise emit a sequence of EntryResources.
                } else {
                    emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Any));
                    foreach (var resource in group.Cast<EntryResource>()) {
                        // If the resource only contains source and possibly version, emit as string.
                        if ((resource.Handler == null) && (resource.MD5 == null) &&
                            (resource.Path == null) && (resource.Side == null) &&
                            (resource.Source.IndexOf('@') < 0)) {
                            var str = resource.Source;
                            if (resource.Version != null)
                                str += $" @ { resource.Version }";
                            emitter.Emit(new Scalar(str));
                        // Otherwise emit the full object.
                        } else ModpackConfig.Serializer.Serialize(emitter, resource);
                    }
                    emitter.Emit(new SequenceEnd());
                }
            }
        }
    }
}
