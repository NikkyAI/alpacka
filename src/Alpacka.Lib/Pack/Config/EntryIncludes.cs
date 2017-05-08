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
            
            // TODO: Implement EntryIncludes.TypeConverter.WriteYaml.
            public void WriteYaml(IEmitter emitter, object value, Type type) =>
                throw new NotImplementedException();
        }
    }
}
