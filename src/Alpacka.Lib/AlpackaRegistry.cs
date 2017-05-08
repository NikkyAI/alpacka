using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alpacka.Lib.Curse;
using Alpacka.Lib.Instances;
using Alpacka.Lib.Instances.MultiMC;
using Alpacka.Lib.Resources;

namespace Alpacka.Lib
{
    public static class AlpackaRegistry
    {
        public static InstanceHandlerCollection InstanceHandlers { get; }
        public static ResourceHandlerCollection ResourceHandlers { get; }
        
        static AlpackaRegistry()
        {
            InstanceHandlers = new InstanceHandlerCollection();
            InstanceHandlers.Register(new ServerHandler());
            InstanceHandlers.Register(new MultiMCHandler());
            
            ResourceHandlers = new ResourceHandlerCollection();
            ResourceHandlers.Register(new ResourceHandlerURL());
            ResourceHandlers.Register(new ResourceHandlerCurse());
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
        
        public class ResourceHandlerCollection : IEnumerable<IResourceHandler>
        {
            private readonly Dictionary<string, IResourceHandler> _handlers
                = new Dictionary<string, IResourceHandler>();
            
            public IResourceHandler this[string name] { get {
                IResourceHandler value = null;
                _handlers.TryGetValue(name.ToLowerInvariant(), out value);
                return value;
            } }
            
            internal ResourceHandlerCollection() {  }
            
            public void Register(IResourceHandler handler) =>
                _handlers.Add(handler.Name.ToLowerInvariant(), handler);
            
            // IEnumerable implementation
            
            public IEnumerator<IResourceHandler> GetEnumerator() => _handlers.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
