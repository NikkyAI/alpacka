namespace Alpacka.Lib.Pack
{
    public class EntryMod : EntryResource
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EntryLinks Links { get; set; }
        
        public EntryMod() => Path = "mods";
        
        public override EntryResource Clone() =>
            new EntryMod {
                Name        = Name,
                Description = Description,
                Links       = Links?.Clone(),
                Handler = Handler,
                Source  = Source,
                MD5     = MD5,
                Version = Version,
                Path    = Path,
                Side    = Side
            };
        
        public static EntryMod Convert(EntryResource resource) =>
            (resource as EntryMod) ?? new EntryMod {
                Source  = resource.Source,
                MD5     = resource.MD5,
                Version = resource.Version,
                Path    = resource.Path,
                Side    = resource.Side
            };
    }
}
