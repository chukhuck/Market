using FaG.Data.DAL;
using System.Threading;

namespace FaG.Common
{
  public interface IFagDownloader
  {
    public Task<List<UserPostEvaluation>> DownloadPostsAsync(DateTime start, DateTime end, CancellationToken token = default);
  }
}
