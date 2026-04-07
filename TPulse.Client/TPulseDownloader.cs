using FaG.Data.Common;
using FaG.Data.DAL;

namespace TPulse.Client
{
  public class TPulseDownloader : IFagDownloader
  {
    public string? Cursor { get; set; }

    private readonly TPulseApiClient _pulseClient;

    public TPulseDownloader(TPulseApiClient pulseApiClient)
    {
      ArgumentNullException.ThrowIfNull(pulseApiClient);

      _pulseClient = pulseApiClient;
    }
    public async Task<List<UserPost>> DownloadPostsAsync(DateTime start, DateTime end, CancellationToken token = default)
    {
      var posts = new List<UserPost>();
      bool flag = true;

      while (flag && !token.IsCancellationRequested)
      {
        try
        {
          var response = await _pulseClient.GetPostsAsync(50, Cursor);
          if (response.Payload?.Items == null || response.Payload.Items.Count == 0)
            break;

          foreach (var post in response.Payload.Items.OrderByDescending(i => i.Inserted))
          {
            if (post.Inserted > end)
              continue;
            else if (post.Inserted < start)
            {
              flag = false;
              break;
            }
            else
            {
              posts.Add(post.ToUserPost());
            }
          }

          if (flag)
            Cursor = response.Payload.NextCursor;
        }
        catch
        {
          break;
        }
      }

      return posts;
    }
  }
}
