using System;
using System.IO;
using IOPath = System.IO.Path;
using Newtonsoft.Json;

namespace Alpacka.Lib.Net
{
    public class DownloadedFile
    {
        /// <summary> Original download URL of the file. </summary>
        public string URL { get; }
        /// <summary> Path of the downloaded file on disk. </summary>
        [JsonIgnore]
        public string Path { get; internal set; }
        /// <summary> Original file name of the downloaded file suggested by the webserver (may be null). </summary>
        public string FileName { get; }
        
        /// <summary> LastModified HTTP field of the downloaded file (if any). </summary>
        public DateTimeOffset? LastModified { get; }
        /// <summary> HTTP ETag of the downloaded file (if any). </summary>
        public string ETag { get; }
        /// <summary> MD5 hash of the downloaded file. </summary>
        public string MD5 { get; }
        
        public DownloadedFile(string url, string path, string fileName,
                              DateTimeOffset? lastModified, string eTag, string md5)
        {
            URL = url; Path = path; FileName = fileName;
            LastModified = lastModified; ETag = eTag; MD5 = md5;
        }
        
        public DownloadedFile Move(string destination)
        {
            var retries = 0;
            var originalName = IOPath.GetFileNameWithoutExtension(destination);
            while (File.Exists(destination)) {
                if (++retries > 5) throw new IOException(
                    $"Could not move downloaded file to '{ destination }' (gave up after 5 retries)");
                destination = IOPath.Combine(
                    IOPath.GetDirectoryName(destination),
                    $"{ originalName } ({ retries }).{ IOPath.GetExtension(destination) }");
            }
            File.Move(Path, destination);
            Path = destination;
            return this;
        }
    }
}
