using System.Collections;
using System.Collections.Generic;
using Alpacka.Lib.Instances;
using Alpacka.Lib.Instances.MultiMC;

namespace Alpacka.Lib
{
    public static class AlpackaRegistry
    {
        public static HandlerCollection InstanceHandlers { get; } = new HandlerCollection();
        
        static AlpackaRegistry()
        {
            InstanceHandlers.Register(new ServerHandler());
            InstanceHandlers.Register(new MultiMCHandler(@"C:\D\games\minecraft\MultiMC")); // FIXME: !!
        }
        
        public class HandlerCollection : IEnumerable<IInstanceHandler>
        {
            private readonly Dictionary<string, IInstanceHandler> _handlers
                = new Dictionary<string, IInstanceHandler>();
            
            public IInstanceHandler this[string name] { get {
                IInstanceHandler value = null;
                _handlers.TryGetValue(name.ToLowerInvariant(), out value);
                return value;
            } }
            
            internal HandlerCollection() {  }
            
            public void Register(IInstanceHandler handler) =>
                _handlers.Add(handler.Name.ToLowerInvariant(), handler);
            
            // IEnumerable implementation
            
            public IEnumerator<IInstanceHandler> GetEnumerator() => _handlers.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
