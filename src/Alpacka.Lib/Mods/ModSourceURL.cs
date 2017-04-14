using System;
using System.Threading.Tasks;
using Alpacka.Lib.Config;

namespace Alpacka.Lib.Mods
{
    public class ModSourceURL : IModSource
    {
        public bool CanHandle(string source) =>
            (source.StartsWith("http://") || source.StartsWith("https://"));
        
        public Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency) =>
            Task.FromResult(mod.Source);
    }
}
