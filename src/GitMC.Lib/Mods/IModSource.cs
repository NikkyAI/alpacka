using System.IO;
using System.Threading.Tasks;
using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public interface IModSource
    {
        /// <summary> Returns if this mod source handles the specified scheme. </summary>
        bool CanHandle(string scheme);
        
        /// <summary> Resolves as much mod information as possible before downloading
        ///           the mod, allowing for some error checking and notifying the
        ///           DependencyHandler of the mod's dependencies. </summary>
        Task Resolve(EntryMod mod, string mcVersion, IDependencyHandler dependencies);
        
        /// <summary> Downloads the mod into the provided stream. </summary>
        Task Download(EntryMod mod, Stream destination);
    }
}
