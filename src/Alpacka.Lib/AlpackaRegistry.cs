using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alpacka.Lib.Curse;
using Alpacka.Lib.Instances;
using Alpacka.Lib.Instances.MultiMC;
using Alpacka.Lib.Mods;

namespace Alpacka.Lib
{
    public static class AlpackaRegistry
    {
        public static InstanceHandlerCollection InstanceHandlers { get; }
        public static SourceHandlerCollection SourceHandlers { get; }
        
        static AlpackaRegistry()
        {
            InstanceHandlers = new InstanceHandlerCollection();
            InstanceHandlers.Register(new ServerHandler());
            InstanceHandlers.Register(new MultiMCHandler());
            
            SourceHandlers = new SourceHandlerCollection();
            SourceHandlers.Register(new ModSourceURL());
            SourceHandlers.Register(new ModSourceCurse());
        }
        
        public class InstanceHandlerCollection : IEnumerable<IInstanceHandler>
        {
            private readonly Dictionary<string, IInstanceHandler> _handlers
                = new Dictionary<string, IInstanceHandler>();
            
            public IInstanceHandler this[string name] { get {
                IInstanceHandler value = null;
                _handlers.TryGetValue(name.ToLowerInvariant(), out value);
                return value;
            } }
            
            internal InstanceHandlerCollection() {  }
            
            public void Register(IInstanceHandler handler) =>
                _handlers.Add(handler.Name.ToLowerInvariant(), handler);
            
            // IEnumerable implementation
            
            public IEnumerator<IInstanceHandler> GetEnumerator() => _handlers.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        public class SourceHandlerCollection : IEnumerable<IModSource>
        {
            private readonly List<IModSource> _handlers = new List<IModSource>();
            
            internal SourceHandlerCollection() {  }
            
            public void Register(IModSource handler) =>
                _handlers.Add(handler);
            
            public IModSource Find(string modSource) =>
                _handlers.First(handler => handler.CanHandle(modSource));
            
            // IEnumerable implementation
            
            public IEnumerator<IModSource> GetEnumerator() => _handlers.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
