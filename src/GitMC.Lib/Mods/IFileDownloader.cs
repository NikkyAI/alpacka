using System.Threading.Tasks;

namespace GitMC.Lib.Mods
{
    public interface IFileDownloader
    {
        /// <summary> Downloads a file from the specified url. </summary>
        Task<DownloadedFile> Download(string url);
    }
    
    public class DownloadedFile
    {
        /// <summary> Original download URL of the file. </summary>
        public string URL { get; }
        /// <summary> Path of the downloaded file on disk. </summary>
        public string Path { get; }
        /// <summary> Original file name of the downloaded file suggested by the webserver (may be null). </summary>
        public string FileName { get; }
        /// <summary> Size of the downloaded file in bytes. </summary>
        public int Size { get; }
        /// <summary> MD5 hash of the downloaded file. </summary>
        public string MD5 { get; }
        
        public DownloadedFile(string url, string path, string fileName, int size, string md5) {
            URL = url;
            Path = path;
            FileName = fileName;
            Size = size;
            MD5 = md5;
        }
    }
}
