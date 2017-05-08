using System;
using System.Threading.Tasks;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Resources
{
    public interface IResourceHandler
    {
        /// <summary> Gets a unique name for this resource handler. </summary>
        string Name { get; }
        
        /// <summary> Called once before any calls to Resolve, allowing this mod
        ///           source to initialize resources once which might take time. </summary>
        Task Initialize();
        
        /// <summary> Returns if this resource handler should be used instead
        ///           for this source string instead of the current default. </summary>
        bool ShouldOverwriteHandler(string source);
        
        /// <summary> Resolves as much information about the resource as possible before
        ///           downloading, allowing for some error checking and adding dependencies.
        ///           Returns the resolved resources with filled out information.
        ///           May be EntryMod, or null if resource should be skipped. </summary>
        Task<EntryResource> Resolve(EntryResource resource, string mcVersion, Action<EntryResource> addDependency);
    }
}
