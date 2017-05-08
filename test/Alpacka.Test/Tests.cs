using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Alpacka.Lib.Pack;
using Alpacka.Lib.Curse;
using Alpacka.Lib.Net;
using Alpacka.Lib.Utility;

namespace Alpacka.Test
{
    public class Tests
    {
        public Tests()
        {
            // Find the workspace root directory by searching for the "alpacka.sln" file.
            var dir = Directory.GetCurrentDirectory();
            while (true) {
                if (File.Exists(Path.Combine(dir, "alpacka.sln"))) break;
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
        public async void DownloadCurse()
        {
            await Task.WhenAll(
                CurseMeta.GetAddon(223008),
                CurseMeta.GetAddon(257572),
                CurseMeta.GetAddonDescription(257572),
                CurseMeta.GetAddonDescription(223008),
                CurseMeta.GetAddonFiles(257572),
                CurseMeta.GetAddonFiles(223008),
                CurseMeta.GetAddonFile(257572, 2382299),
                CurseMeta.GetAddonFileChangelog(257572, 2382299)
            );
        }
        
        [Fact]
        public async void TestCurseClasses()
        {
            // Compare values from LatestProjects with values received from the RestProxy
            
            var latest = await ProjectFeed.Get();
            var rnd = new Random();
            var randomData = latest.Data.OrderBy(x => rnd.Next()).ToList();
            
            async Task TestAddon(Addon addon)
            {
                var realAddon = await CurseMeta.GetAddon(addon.Id);
                Assert.Equal(addon.Status, realAddon.Status);
                Assert.Equal(addon.Stage, realAddon.Stage);
                Assert.Equal(addon.PackageType, realAddon.PackageType);
                for (int i = 0; i < addon.GameVersionLatestFiles.Length; i++)
                    Assert.Equal(addon.GameVersionLatestFiles[i].FileType, realAddon.GameVersionLatestFiles[i].FileType);
                
                foreach (var file in addon.LatestFiles) {
                    var realFile = await CurseMeta.GetAddonFile(addon.Id, file.Id);
                    for (int i = 0; i < file.Dependencies.Length; i++)
                        Assert.Equal(file.Dependencies[i].Type, realFile.Dependencies[i].Type);
                    
                    Assert.Equal(file.FileStatus, realFile.FileStatus);
                    Assert.Equal(file.ReleaseType, realFile.ReleaseType);
                }
            }
            
            // Test with 100 random Addons.
            
            var batchSize = 10;
            var all = randomData.Take(100)
                .Select((addon, index) => new { addon, index })
                .GroupBy(e => (e.index / batchSize), e => e.addon);
            
            foreach (var batch in all) {
                await Task.WhenAll(batch.Select(TestAddon));
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
        
        [Fact]
        public async void ForgeInstaller()
        {
            var forgeData = await ForgeVersionData.Download();
            string url = forgeData.GetRecent("1.10.2", Release.Recommended)
                ?.GetInstaller()?.GetURL();
            Console.WriteLine($"URL: { url }");
        }
    }
}
