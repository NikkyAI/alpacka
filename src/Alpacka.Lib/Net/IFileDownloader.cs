using System.Threading.Tasks;

namespace Alpacka.Lib.Net
{
    public interface IFileDownloader
    {
        /// <summary> Downloads a file from the specified URL using the supplied
        ///           relativePath (defaulting to the server's suggested filename). </summary>
        Task<DownloadedFile> Download(string url, string relativePath = null);
    }
}
