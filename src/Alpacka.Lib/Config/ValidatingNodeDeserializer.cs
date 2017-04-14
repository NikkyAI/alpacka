using System;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Config
{
    public class ValidatingNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer _nodeDeserializer;
        
        public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer)
            { _nodeDeserializer = nodeDeserializer; }
        
        public bool Deserialize(IParser reader, Type expectedType,
                                Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (_nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) {
                var context = new ValidationContext(value, null, null);
                Validator.ValidateObject(value, context, true);
                return true;
            } else return false;
        }
    }
}
