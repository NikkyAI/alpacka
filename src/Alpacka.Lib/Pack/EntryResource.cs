using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Alpacka.Lib.Pack
{
    public class EntryResource
    {
        /// <summary> Name of the ISourceHandler to use for
        ///           this resource if Source is ambiguous. </summary>
        public string Handler { get; set; }
        
        /// <summary> Source of the resource. May be a URL.
        ///           If ambiguous, the current handler is used. </summary>
        [Required, YamlMember(Alias = "src"), JsonProperty("src")]
        public string Source { get; set; }
        
        /// <summary> MD5 hash of the file, used for verification. </summary>
        public string MD5 { get; set; }
        
        /// <summary> Version of the resource, if any.
        ///           May also be a Release string.
        ///           Currently only applies to mods. </summary>
        public string Version { get; set; }
        
        /// <summary> Destination (and sometimes relative
        ///           source) path of the resource. </summary>
        public string Path { get; set; }
        
        /// <summary> Side of the resource. If not Both, it
        ///           will be only be available on this side. </summary>
        public Side? Side { get; set; }
        
        
        public virtual EntryResource Clone() =>
            new EntryResource {
                Version = Version,
                Source  = Source,
                MD5     = MD5,
                Side    = Side
            };
        
        public static implicit operator EntryResource(string value)
        {
            var atIndex = value.IndexOf('@');
            return (atIndex < 0)
                ? new EntryResource { Source = value }
                : new EntryResource {
                    Source  = value.Substring(0, atIndex),
                    Version = value.Substring(atIndex - 1)
                };
        }
    }
}
