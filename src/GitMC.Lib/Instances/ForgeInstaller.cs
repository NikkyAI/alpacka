using System;
using System.IO;
using System.Threading.Tasks;
using GitMC.Lib.Config;
using GitMC.Lib.Net;
using GitMC.Lib.Util;
using GitMC.Lib.Instances.MultiMC;

namespace GitMC.Lib.Instances
{
    public static class ForgeInstaller
    {
        public static async Task<string> InstallServer(string directory, ModpackBuild build, ForgeVersion forge)
        {
            string forgeUniversalFile = Path.Combine(directory, $"forge-{ build.MinecraftVersion }-{ build.ForgeVersion }-universal.jar");
            string forgeFile = Path.Combine(directory, $"forge_server.jar");
            
            if (!File.Exists(forgeUniversalFile)) {
                var url = forge.GetInstaller().GetURL();
                DownloadedFile installerFile;
                using (var fileCache = new FileCache(Path.Combine(Constants.CachePath, "forge")))
                using (var downloader = new FileDownloaderURL(fileCache))
                    installerFile = await downloader.Download(url);
                
                Directory.SetCurrentDirectory(directory);
                
                var status = await ThreadUtil.RunProcessAsync("java", $"-jar \"{ installerFile.Path }\" --installServer");
                
                Console.WriteLine();
                Console.WriteLine($"SUCCESS: Installed { forgeUniversalFile } with status { status }");
            }
            
            File.Copy(forgeUniversalFile, forgeFile, true);
            return forgeFile;
        }
        
        public static async Task<string> InstallMultiMC(string directory, ModpackBuild build)
        {
            // install forge
            var forgePatch = MultiMCMeta.GetForgePatch($"{ build.MinecraftVersion }-{ build.ForgeVersion }");
            var patchFolder = Path.Combine(directory, "patches");
            Directory.CreateDirectory(patchFolder);
            File.WriteAllText(Path.Combine(patchFolder, "net.minecraftforge.json"), forgePatch);
            Console.WriteLine($"installed forge { build.ForgeVersion }");
            
            return build.ForgeVersion;
        }
    }
}