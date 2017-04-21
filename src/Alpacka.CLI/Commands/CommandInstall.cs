using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Alpacka.Lib;
using Alpacka.Lib.Git;
using Alpacka.Lib.Utility;
using Microsoft.Extensions.CommandLineUtils;

namespace Alpacka.CLI.Commands
{
    public class CommandInstall : CommandLineApplication
    {
        public CommandInstall()
        {
            Name = "install";
            Description = "Installs a new alpacka pack instance";
            
            var argType = Argument("type",
                "The type of instance can be 'Vanilla', 'Server' or 'MultiMC'");
            var argRepository = Argument("repository",
                "The git repository to install the instance from");
            var argDirectory = Argument("[directory]",
                "The directory to install the instance into");
            
            HelpOption("-? | -h | --help");
            
            OnExecute(() => {
                var instanceHandler = AlpackaRegistry.InstanceHandlers[argType.Value];
                if (instanceHandler == null) {
                    Console.WriteLine($"ERROR: No handler for type '{ argType.Value }'");
                    return 1;
                }
                
                // Is the supplied directory a name or a relative / absolute path?
                var directoryIsPath = (argDirectory.Value != null) &&
                    ((argDirectory.Value == ".") || (argDirectory.Value == "..") ||
                     (argDirectory.Value.IndexOfAny(@"/\".ToCharArray()) != -1));
                var directory = directoryIsPath ? new DirectoryInfo(argDirectory.Value) : null;
                
                // Get a temporary clone path. We clone into a temp path because the
                // actual path might be unknown, since it may depend on the pack name.
                var tempDirName   = Guid.NewGuid().ToString();
                var tempClonePath = directoryIsPath
                    ? directory.Parent.CreateSubdirectory(tempDirName).FullName
                    : instanceHandler.GetInstancePath(tempDirName);
                
                try {
                    
                    Console.WriteLine("Cloning / downloading git repository ...");
                    Debug.WriteLine($"Cloning into: { tempClonePath }");
                    InstallUtil.Clone(argRepository.Value, tempClonePath);
                    
                    var pack = CommandUpdate.GetBuild(tempClonePath).Result;
                    
                    var safeName = string.Join("_", pack.Name.Split(Path.GetInvalidPathChars()));
                    var instancePath = directoryIsPath
                        ? directory.FullName
                        : instanceHandler.GetInstancePath(safeName);
                    Debug.WriteLine($"Instance path: { instancePath }");
                    
                    if (Directory.Exists(instancePath)) {
                        Console.WriteLine($"ERROR: The instance path '{ instancePath }' already exists");
                        return 1;
                    }
                    
                    // Ensure the parent directory exists.
                    Directory.CreateDirectory(Path.GetDirectoryName(instancePath));
                    // Now move the temp directory to the proper location.
                    Directory.Move(tempClonePath, instancePath);
                    
                    Debug.WriteLine($"Installing using instance handler { instanceHandler.Name }");
                    instanceHandler.Install(instancePath, pack);
                    
                    var info = new AlpackaInfo { InstanceType = instanceHandler.Name };
                    info.Save(instancePath);
                    
                    Console.WriteLine($"Successfully installed new { instanceHandler.Name } instance into '{ instancePath }'");
                    
                    return CommandUpdate.Execute(instancePath, pack).Result;
                    
                } finally {
                    // Delete the temporarily cloned directory
                    // if any problems occured before it was moved.
                    var tempDir = new DirectoryInfo(tempClonePath);
                    if (tempDir.Exists) {
                        tempDir.ClearReadOnly();
                        tempDir.Delete(true);
                    }
                    // Clear up any empty parent directories.
                    while (!(tempDir = tempDir.Parent).EnumerateFileSystemInfos().Any())
                        tempDir.Delete();
                }
            });
        }
    }
}