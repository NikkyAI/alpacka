using System.Threading.Tasks;

namespace Alpacka.Lib.Net
{
    public interface IFileDownloader
    {
        /// <summary> Downloads a file from the specified URL. </summary>
        Task<DownloadedFile> Download(string url);
    }
}
