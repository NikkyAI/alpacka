namespace Alpacka.Lib.Config
{
    public class EntryDefaults
    {
        public static readonly EntryDefaults Default = new EntryDefaults();
        
        public Release Version { get; set; } = Release.Recommended;
    }
}
