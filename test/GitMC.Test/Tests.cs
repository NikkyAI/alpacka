using System;
using System.IO;
using Xunit;
using GitMC.Lib.Config;
using GitMC.Lib.Net;

namespace GitMC.Test
{
    public class Tests
    {
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
        public async void ForgeInstaller()
        {
            var forgeData = await ForgeVersionData.Download();
            string url = forgeData.GetRecent("1.10.2", DefaultVersion.Recommended)
                ?.GetInstaller()?.GetURL();
            Console.WriteLine($"URL: { url }");
        }
    }
}
