using System;
using System.IO;
using LibGit2Sharp;

namespace Alpacka.Lib.Git
{
    public static class InstallUtil
    {
        public static string Clone(string url, string workingDirectory)
        {
            var directory = Path.Combine(workingDirectory, $"{ Guid.NewGuid() }");
            Repository.Clone(url, directory);
            
            return directory;
        }
    }
}