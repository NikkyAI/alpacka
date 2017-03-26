using System.IO;

namespace GitMC.Lib.Net
{
    public class DownloadedFile
    {
        /// <summary> Original download URL of the file. </summary>
        public string URL { get; }
        /// <summary> Path of the downloaded file on disk. </summary>
        public string Path { get; private set; }
        /// <summary> Original file name of the downloaded file suggested by the webserver (may be null). </summary>
        public string FileName { get; }
        /// <summary> MD5 hash of the downloaded file. </summary>
        public string MD5 { get; }
        
        public DownloadedFile(string url, string path, string fileName, string md5)
            { URL = url; Path = path; FileName = fileName; MD5 = md5; }
        
        public DownloadedFile Move(string destination, bool replace = false)
        {
            if (replace && File.Exists(destination))
                File.Delete(destination);
            File.Move(Path, destination);
            Path = destination;
            return this;
        }
    }
}
