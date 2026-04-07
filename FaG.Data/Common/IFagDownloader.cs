using FaG.Data.DAL;

namespace FaG.Data.Common
{
  public interface IFagDownloader
  {
    string? Cursor { get; set; }

    public Task<List<UserPost>> DownloadPostsAsync(DateTime start, DateTime end, CancellationToken token = default);
  }
}
