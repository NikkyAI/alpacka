using System;
using System.IO;
using Xunit;
using GitMC.Lib.Config;
using GitMC.Lib.Mods;

namespace GitMC.Test
{
    public class Tests
    {
        public static readonly String CONFIG_FILE = "packconfig.yaml";
        public static readonly String BUILD_FILE  = "packbuild.json";
        public static readonly String MODS_FOLDER  = "mods/";
        
        public Tests()
        {
            // Find the workspace root directory by searching for the "gitmc.sln" file.
            var dir = Directory.GetCurrentDirectory();
            while (true) {
                if (File.Exists(Path.Combine(dir, "gitmc.sln"))) break;
                try { dir = Directory.GetParent(dir).FullName; }
                catch { throw new Exception("Workspace root directory not found"); }
            }
            // Set current working directory to the "run" directory
            // in the workspace root, creating it if necessary.
            var cwd = Path.Combine(dir, "run");
            Directory.CreateDirectory(cwd);
            Directory.SetCurrentDirectory(cwd);
        }
        
        [Fact]
        public async void LoadDownloadBuild()
        {
            var config = ModpackConfig.Load(CONFIG_FILE);
            
            var downloader = new ModpackDownloader()
                .WithSourceHandler(new ModSourceURL());
            var downloaded = await downloader.Run(config);
            
            if (Directory.Exists(MODS_FOLDER))
                Directory.Delete(MODS_FOLDER, true);
            Directory.CreateDirectory(MODS_FOLDER);
            
            foreach (var downloadedMod in downloaded) 
                File.Move(downloadedMod.File.Path, Path.Combine(MODS_FOLDER,
                    (downloadedMod.File.FileName ?? $"{ downloadedMod.Mod.Name }-{ downloadedMod.Mod.Version }.jar")));
            
            var build = new ModpackBuild(config);
            build.Save(BUILD_FILE, pretty: true);
        }
    }
}
