using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Alpacka.Lib.Net;
using Alpacka.Lib.Pack;

namespace Alpacka.Lib.Instances
{
    public class ServerHandler : IInstanceHandler
    {
        public string Name => "Server";
        public Side Side => Side.Server;
        
        public string GetInstancePath(string instanceName) =>
            Path.Combine(Directory.GetCurrentDirectory(), instanceName);
        
        public List<string> GetInstances() => null;
        
        
        public void Install(string instancePath, ModpackBuild pack)
        {
            // TODO: Create startup scripts (.sh and .bat).
            Update(instancePath, null, pack);
        }
        
        public void Update(string instancePath, ModpackBuild oldPack, ModpackBuild newPack)
        {
            var forgeData    = ForgeVersionData.Download().Result;
            var forgeVersion = forgeData[newPack.ForgeVersion];
            
            // FIXME: Find old forge version, delete it, rename the newly downloaded one.
            // var universalPath = Path.Combine(instancePath, $"forge-{ newPack.MinecraftVersion }-{ newPack.ForgeVersion }-universal.jar");
            // var forgePath     = Path.Combine(instancePath, $"forge-universal.jar");
            
            // Download the Forge installer.
            var forgeURL = forgeVersion.GetInstaller().GetURL();
            DownloadedFile installerFile; // TODO: Move cache related code somewhere else?
            using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "forge")))
            using (var downloader = new FileDownloaderURL(fileCache))
                installerFile = downloader.Download(forgeURL).Result;
            
            // Run it.
            var startInfo = new ProcessStartInfo {
                FileName  = "java", // TODO: Allow specifiying java bin path?
                Arguments = $"-jar \"{ installerFile.FullPath }\" --installServer", // FIXME: Escape path!
                WorkingDirectory = instancePath
            };
            using (var process = Process.Start(startInfo)) {
                process.WaitForExit();
                // TODO: Proper error handling. (Redirect process output?)
                if (process.ExitCode != 0) throw new Exception(
                    $"Failed to install Forge server (exit code { process.ExitCode })");
            }
        }
        
        public void Remove(string instancePath)
        {
            Directory.Delete(instancePath, true);
        }
    }
}
