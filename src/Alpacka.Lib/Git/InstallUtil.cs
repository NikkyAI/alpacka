using LibGit2Sharp;

namespace Alpacka.Lib.Git
{
    public static class InstallUtil
    {
        public static void Clone(string url, string clonePath)
        {
            Repository.Clone(url, clonePath);
        }
    }
}