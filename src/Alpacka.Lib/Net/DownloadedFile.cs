using System;
using System.IO;
using Newtonsoft.Json;

namespace Alpacka.Lib.Net
{
    public class DownloadedFile
    {
        /// <summary> Original download URL of the file. </summary>
        public string URL { get; }
        /// <summary> Path of the downloaded file on disk. </summary>
        [JsonIgnore]
        public string FullPath { get; internal set; }
        /// <summary> Relative file path of the downloaded file
        ///           (for example "projects/50002/files.json"). </summary>
        public string RelativePath { get; }
        /// <summary> Original file name of the downloaded file
        ///           (might be suggested by the webserver). </summary>
        [JsonIgnore]
        public string FileName => Path.GetFileName(RelativePath);
        
        /// <summary> LastModified HTTP field of the downloaded file (if any). </summary>
        public DateTimeOffset? LastModified { get; }
        /// <summary> HTTP ETag of the downloaded file (if any). </summary>
        public string ETag { get; }
        /// <summary> MD5 hash of the downloaded file. </summary>
        public string MD5 { get; }
        
        public DownloadedFile(string url, string path, string relativePath,
                              DateTimeOffset? lastModified, string eTag, string md5)
        {
            URL = url; FullPath = path; RelativePath = relativePath;
            LastModified = lastModified; ETag = eTag; MD5 = md5;
        }
        
        public DownloadedFile Move(string destination)
        {
            var retries = 0;
            var originalName = Path.GetFileNameWithoutExtension(destination);
            while (File.Exists(destination)) {
                if (++retries > 5) throw new IOException(
                    $"Could not move downloaded file to '{ destination }' (gave up after 5 retries)");
                destination = Path.Combine(
                    Path.GetDirectoryName(destination),
                    $"{ originalName } ({ retries }).{ Path.GetExtension(destination) }");
            }
            File.Move(FullPath, destination);
            FullPath = destination;
            return this;
        }
        
        public override string ToString() => RelativePath;
    }
}
