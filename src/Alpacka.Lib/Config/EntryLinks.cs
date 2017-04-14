namespace Alpacka.Lib.Config
{
    public class EntryLinks
    {
        public string Website { get; set; }
        public string Source { get; set; }
        public string Issues { get; set; }
        public string Donations { get; set; }
        
        public EntryLinks Clone() =>
            new EntryLinks {
                Website   = Website,
                Source    = Source,
                Issues    = Issues,
                Donations = Donations
            };
    }
}
