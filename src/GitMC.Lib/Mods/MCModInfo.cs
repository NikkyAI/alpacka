using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GitMC.Lib.Mods
{
    public class MCModInfo
    {
        // For information on what each field does, see:
        // https://github.com/MinecraftForge/FML/wiki/FML-mod-information-file
        
        [JsonProperty("modid")]
        public string ModID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        [JsonProperty("mcversion")]
        public string MinecraftVersion { get; set; }
        public string URL { get; set; }
        public string UpdateURL { get; set; }
        public List<string> AuthorList { get; set; } = new List<string>();
        public string Credits { get; set; }
        public string LogoFile { get; set; }
        public List<string> Screenshots { get; set; } = new List<string>();
        public string Parent { get; set; }
        public List<string> RequiredMods { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Dependants { get; set; } = new List<string>();
        public bool UseDependencyInformation { get; set; }
        
        public static async Task<MCModInfo> Extract(Stream modFileStream)
        {
            var zip   = new ZipFile(modFileStream);
            var entry = zip.GetEntry("mcmod.info");
            if (entry == null) throw new Exception("mcmod.info could not be found in mod archive");
            
            string modInfo;
            using (var reader = new StreamReader(zip.GetInputStream(entry)))
                modInfo = await reader.ReadToEndAsync();
            
            var settings = new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver() };
            
            List<MCModInfo> infos = (modInfo[0] == '{')
                ? JsonConvert.DeserializeObject<InfoList>(modInfo, settings).ModList // New format
                : JsonConvert.DeserializeObject<List<MCModInfo>>(modInfo, settings); // Old format
            if (infos == null) throw new Exception("mcmod.info doesn't contain a 'modList' entry");
            if (infos.Count == 0) throw new Exception("mcmod.info contains 0 mod info entries");
            return infos[0];
        }
        
        public class InfoList
        {
            public int ModListVersion { get; set; }
            public List<MCModInfo> ModList { get; set; }
        }
    }
}
