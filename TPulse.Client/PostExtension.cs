using FaG.Data.DAL;
using TPulse.Client.Model;

namespace TPulse.Client
{
  public static class PostExtension
  {
    public static UserPost ToUserPost(this Post post)
    {
      return new UserPost
      {
        InnerId = post.Id.ToString(),
        Source = "TPulse",
        Date = post.Inserted,
        AuthorId = post.Owner?.Id ?? Guid.Empty,
        AuthorNickname = post.Owner?.Nickname ?? "Unknown",
        Text = post?.Content?.Text ?? string.Empty,
        Lenght = post?.Content?.Text?.Length ?? 0,
        CommentsCount = post?.CommentsCount ?? 0,
        TotalReactions = post?.Reactions?.TotalCount ?? 0,
        ReactionsJson = post is null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(post.Reactions),
        Tickers = post is null ? string.Empty : string.Join(",", post.Content?.Instruments?.Select(i => i.Ticker).Where(t => !string.IsNullOrEmpty(t)) ?? Enumerable.Empty<string>())
      };
    }
  }
}
