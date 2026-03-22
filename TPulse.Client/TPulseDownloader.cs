using FaG.Common;
using FaG.Data.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    public async Task<List<UserPostEvaluation>> DownloadPostsAsync(DateTime start, DateTime end, CancellationToken token = default)
    {     
      var posts = new List<UserPostEvaluation>();
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
