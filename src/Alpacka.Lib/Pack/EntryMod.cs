namespace Alpacka.Lib.Pack
{
    public class EntryMod : EntryResource
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EntryLinks Links { get; set; }
        
        public override EntryResource Clone() =>
            new EntryMod {
                Name        = Name,
                Description = Description,
                Links       = Links?.Clone(),
                Version = Version,
                Source  = Source,
                MD5     = MD5,
                Side    = Side,
            };
        
        public static EntryMod Convert(EntryResource resource) =>
            (resource as EntryMod) ?? new EntryMod {
                Version = resource.Version,
                Source  = resource.Source,
                MD5     = resource.MD5,
                Side    = resource.Side
            };
    }
}
