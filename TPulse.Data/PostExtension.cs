using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPulse.Data
{
  public static class PostExtension
  {
    public static UserPostEvaluation ToPostEvaluation(this TPulse.Client.Model.Post post, Emotion emotion)
    {
      return new UserPostEvaluation
      {
        PostId = post.Id,
        EvaluationDate = DateTime.Now,
        Emotion = emotion,
        AuthorId = post.Owner?.Id ?? Guid.Empty,
        AuthorNickname = post.Owner?.Nickname ?? "Unknown",
        PostText = post?.Content?.Text ?? string.Empty,
        CommentsCount = post?.CommentsCount ?? 0,
        TotalReactions = post?.Reactions?.TotalCount ?? 0,
        ReactionsJson = post is null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(post.Reactions),
        Tickers = post is null ? string.Empty : string.Join(",", post.Content?.Instruments?.Select(i => i.Ticker).Where(t => !string.IsNullOrEmpty(t)) ?? Enumerable.Empty<string>())
      };
    }
  }
}
