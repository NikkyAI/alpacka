namespace GitMC.Lib.Config
{
    public class EntryDefaults
    {
        public static readonly EntryDefaults Default = new EntryDefaults();
        
        public DefaultVersion Version { get; set; } = DefaultVersion.Recommended;
    }
}
