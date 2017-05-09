using System.Collections.Generic;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Instances
{
    public interface IInstanceHandler
    {
        /// <summary> Gets a unique name for this instance type. </summary>
        string Name { get; }
        
        Side Side { get; }
        
        /// <summary> Gets the full path of an instance for this type with
        ///           the specified simplified (safe) instance / folder name. </summary>
        string GetInstancePath(string instanceName);
        
        /// <summary> Gets the full path of an instance for this type with
        ///           the specified simplified (safe) instance / folder name. </summary>
        string GetInstancePath(string instanceName, string basedir);
        
        /// <summary> Returns the list of instance paths that are installed
        ///           for this instance type. Returns null if not supported. </summary>
        List<string> GetInstances();
        
        
        /// <summary>
        ///   Installs a new instance in the specified path
        ///   for this type using the specified modpack data.
        ///   
        ///   Handles Forge and instance metadata, not installing mods / configs.
        /// </summary>
        void Install(string instancePath, ModpackBuild pack);
        
        /// <summary>
        ///   Updates the instance in the specified path.
        ///   
        ///   Handles Forge and instance metadata, not updating mods / configs.
        /// </summary>
        // FIXME: Currently, oldPack isn't required at the moment but would be useful.
        void Update(string instancePath, ModpackBuild oldPack, ModpackBuild newPack);
        
        /// <summary>
        ///   Removes the instance in the specified path.
        /// </summary>
        void Remove(string instancePath);
    }
}
