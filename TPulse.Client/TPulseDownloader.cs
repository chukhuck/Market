using FaG.Data.Common;
using FaG.Data.DAL;

namespace TPulse.Client
{
  public class TPulseDownloader : IFagDownloader
  {
    private readonly TPulseApiClient _pulseClient;
    private string? nextCursor = null;

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
          var response = await _pulseClient.GetPostsAsync(50, nextCursor);
          if (response.Payload?.Items == null || response.Payload.Items.Count == 0)
            break; 

          foreach (var post in response.Payload.Items.OrderByDescending(i => i.Inserted))
          {
            if (post.Inserted > end)
              continue;

            if (post.Inserted < start)
               flag = false;

            posts.Add(post.ToUserPost(emotion: Emotion.None));
          }

          nextCursor = response.Payload.NextCursor;
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
