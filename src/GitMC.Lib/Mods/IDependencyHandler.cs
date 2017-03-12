using GitMC.Lib.Config;

namespace GitMC.Lib.Mods
{
    public interface IDependencyHandler
    {
        void Add(EntryMod dependency);
    }
}
