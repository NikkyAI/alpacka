using System;
using System.IO;
using System.Threading.Tasks;
using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public interface IModSource
    {
        /// <summary> Returns if this mod source handles the specified source string. </summary>
        bool CanHandle(string source);
        
        /// <summary> Resolves as much mod information as possible before downloading
        ///           the mod, allowing for some error checking and adding dependencies. </summary>
        Task Resolve(EntryMod mod, string mcVersion, Action<EntryMod> addDependency);
        
        /// <summary> Downloads the mod into the provided stream. Returns the file
        ///           name of the downloaded file (can be null if not available). </summary>
        Task<string> Download(EntryMod mod, Stream destination);
    }
}
