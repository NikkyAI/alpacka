using System;
using System.Threading.Tasks;
using Alpacka.Lib.Config;

namespace Alpacka.Lib.Mods
{
    public interface IModSource
    {
        /// <summary> Returns if this mod source handles the specified source string. </summary>
        bool CanHandle(string source);
        
        /// <summary> Resolves as much mod information as possible before downloading
        ///           the mod, allowing for some error checking and adding dependencies.
        ///           Returns the download URL or null if the mod should be discarded. </summary>
        Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency);
    }
}