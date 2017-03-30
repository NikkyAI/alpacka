using System;
using System.IO;
using System.Threading.Tasks;
using GitMC.Lib;
using GitMC.Lib.Config;
using GitMC.Lib.Net;
using GitMC.Lib.Util;

namespace GitMC.CLI.Commands
{
    public static class ForgeInstaller
    {
        public static async Task<string> InstallServer(string directory, ModpackVersion build, ForgeVersion forge)
        {
            string forgeUnversalFile =  Path.Combine(directory, $"forge-{build.MinecraftVersion}-{build.ForgeVersion}-universal.jar");
            string forgeFile = Path.Combine(directory, $"forge_server.jar");
            if(!File.Exists(forgeUnversalFile))
            {
                var url = forge.GetInstaller().GetURL();
                DownloadedFile installerFile;
                using(var fileCache = new FileCache(Path.Combine(Constants.CachePath, "forge")))
                    using(var downloader = new FileDownloaderURL(fileCache))
                        installerFile = await downloader.Download(url);
                
                Directory.SetCurrentDirectory(directory);
                
                var status = await ThreadUtil.RunProcessAsync("java", $"-jar {installerFile.Path} --installServer");

                // var timespan = new TimeSpan(process.ExitTime.Ticks - process.StartTime.Ticks);
            
                Console.WriteLine($"\nSUCCESS: installed {forgeUnversalFile} with status {status}");
            }
            
            File.Copy(forgeUnversalFile, forgeFile, true);
            return forgeFile;
        }
    }
}