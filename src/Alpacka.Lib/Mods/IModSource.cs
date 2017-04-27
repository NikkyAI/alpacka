using System;
using System.Threading.Tasks;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Mods
{
    public interface IModSource
    {
        /// <summary> Returns if this mod source handles the specified source string. </summary>
        bool CanHandle(string source);
        
        /// <summary> Called once before any calls to Resolve, allowing this mod
        ///           source to initialize resources once which might take time. </summary>
        Task Initialize();
        
        /// <summary> Resolves as much mod information as possible before downloading
        ///           the mod, allowing for some error checking and adding dependencies.
        ///           Returns the download URL or null if the mod should be discarded. </summary>
        Task<string> Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency);
    }
}
