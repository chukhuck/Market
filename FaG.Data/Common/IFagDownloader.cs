using FaG.Data.DAL;

namespace FaG.Common
{
  public interface IFagDownloader
  {
    public Task<List<UserPostEvaluation>> DownloadPostsAsync(DateTime start, DateTime end);
  }
}
