using System;
using System.Threading.Tasks;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Resources
{
    public class ResourceHandlerURL : IResourceHandler
    {
        public string Name => "URL";
        
        public Task Initialize() =>
            Task.CompletedTask;
        
        public bool ShouldOverwriteHandler(string source) =>
            (source.StartsWith("http://") || source.StartsWith("https://"));
        
        public Task<EntryResource> Resolve(EntryResource resource, string mcVersion,
                                           Action<EntryResource> addDependency) =>
            Task.FromResult(resource);
            
        public void Finish() {}
    }
}
