using FaG.Common;
using FaG.Data.DAL;

namespace TPulse.Client
{
  public class TPulseDownloader : IFagDownloader
  {
    public required TPulseApiClient _pulseClient;
    public required FaGDbContext _dbContext;
    private string? nextCursor = null;

    public TPulseDownloader(TPulseApiClient pulseApiClient, FaGDbContext context)
    {
      ArgumentNullException.ThrowIfNull(nameof(pulseApiClient));
      ArgumentNullException.ThrowIfNull(nameof(context));

      _pulseClient = pulseApiClient;
      _dbContext = context;
    }
    public async Task<List<UserPostEvaluation>> DownloadPostsAsync(DateTime start, DateTime end)
    {     
      var posts = new List<UserPostEvaluation>();
      bool flag = true;

      while (flag)
      {
        try
        {
          var response = await _pulseClient.GetPostsAsync(50, nextCursor);
          if (response.Payload?.Items == null || response.Payload.Items.Count == 0)
            break; 

          foreach (var post in response.Payload.Items.OrderByDescending(i => i.Inserted))
          {
            if (post.Inserted <= end)
              flag = false;

            if (post.Inserted < start)
              continue;

            posts.Add(post.ToPostEvaluation(emotion: Emotion.None));
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
