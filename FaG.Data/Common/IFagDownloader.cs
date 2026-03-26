using FaG.Data.DAL;

namespace FaG.Data.Common
{
  public interface IFagDownloader
  {
    public Task<List<UserPost>> DownloadPostsAsync(DateTime start, DateTime end, CancellationToken token = default);
  }
}
