using System.IO;
using Newtonsoft.Json;

namespace Alpacka.Lib
{
    public class AlpackaInfo
    {
        public string InstanceType { get; set; }
        
        public static AlpackaInfo Load(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.INSTANCE_INFO_FILE);
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<AlpackaInfo>(File.ReadAllText(path))
                : null;
        }
        
        public void Save(string path)
        {
            if (Directory.Exists(path))
                path = Path.Combine(path, Constants.INSTANCE_INFO_FILE);
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
    }
}
