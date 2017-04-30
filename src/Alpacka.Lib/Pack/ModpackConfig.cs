using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using Alpacka.Lib.Utility;

namespace Alpacka.Lib.Pack
{
    public class ModpackConfig : Modpack
    {
        private static readonly Deserializer _deserializer =
            new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner),
                                        s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithTypeConverter(new EntriesTypeConverter())
                .Build();
            
        public EntryDefaults Defaults { get; set; }
        
        [Required]
        public Entries Includes { get; set; }
        
        public static ModpackConfig LoadYAML(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.PACK_CONFIG_FILE);
            
            ModpackConfig config;
            using (var reader = new StreamReader(File.OpenRead(path)))
                config = _deserializer.Deserialize<ModpackConfig>(reader);
            
            return config;
        }
        
        
        
        public class Entries : List<Group> {  }
        
        public class Group : List<object>
        {
            public string FullName { get; }
            public Group(string fullName)
                { FullName = fullName; }
        }
        
        public class Feature : Group
        {
            public string Type { get; set; }
            public Feature(string type, string fullName)
                : base(fullName) { Type = type; }
        }
        
        
        public class EntriesTypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type) =>
                typeof(Entries).GetTypeInfo().IsAssignableFrom(type);
            
            public object ReadYaml(IParser parser, Type type)
            {
                var entries = new Entries();
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
                    var list = _deserializer.Deserialize<List<EntryResource>>(parser);
                    group = new Group(fullName);
                    group.AddRange(list);
                } else throw new YamlException(parser.Current.Start, parser.Current.End,
                    $"Expected 'MappingStart' or 'SequenceStart', got '{ parser.Current.GetType().Name }' (at { parser.Current.Start }).");
                return group;
            }
            
            // TODO: Implement EntriesTypeConverter.WriteYaml.
            public void WriteYaml(IEmitter emitter, object value, Type type) =>
                throw new NotImplementedException();
        }
    }
}
