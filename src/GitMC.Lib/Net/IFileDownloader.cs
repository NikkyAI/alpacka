using System.Threading.Tasks;

namespace GitMC.Lib.Net
{
    public interface IFileDownloader
    {
        /// <summary> Downloads a file from the specified url. </summary>
        Task<DownloadedFile> Download(string url);
    }
}
