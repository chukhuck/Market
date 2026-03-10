using TPulse.Data;
using TPulse.Client;
using TPulseHistoryDownloader.DAL;

class Program
{
  private static TPulseApiClient? _pulseClient;
  private static HistoryDbContext? _dbContext;
  private static readonly DateTime _stopDate = new(2025, 1, 1);

  static async Task Main(string[] _args)
  {
    Initialize();
    await DownloadHistoricalPostsAsync();
  }

  private static void Initialize()
  {
    _pulseClient = new TPulseApiClient(
        "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed",
        "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}",
        "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg"
    );

    _dbContext = new HistoryDbContext();
    _dbContext.Database.EnsureCreated();
  }

  private static async Task DownloadHistoricalPostsAsync()
  {
    if (_pulseClient == null || _dbContext == null)
    {
      Console.WriteLine("Ошибка инициализации. Проверьте настройки.");
      return;
    }

    string? nextCursor = null;
    int totalDownloaded = 0;

    Console.WriteLine($"Начинаем загрузку постов до {_stopDate:yyyy-MM-dd}...");

    bool flag = true;
    while (flag)
    {
      try
      {
        var response = await _pulseClient.GetPostsAsync(50, nextCursor);
        if (response.Payload?.Items == null || response.Payload.Items.Count == 0)
          break;

        var postsToSave = new List<UserPostEvaluation>();

        foreach (var post in response.Payload.Items)
        {
          if (post.Inserted >= _stopDate)
            flag = false;

          var evaluation = post.ToPostEvaluation(emotion: Emotion.None);
          postsToSave.Add(evaluation);
        }

        _dbContext.UserPostEvaluations.AddRange(postsToSave);
        await _dbContext.SaveChangesAsync();

        totalDownloaded += postsToSave.Count;
        Console.WriteLine($"Загружено {postsToSave.Count} постов. Всего: {totalDownloaded}");

        nextCursor = response.Payload.NextCursor;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка загрузки: {ex.Message}");
        break;
      }
    }

    Console.WriteLine($"Загрузка завершена. Всего загружено: {totalDownloaded} постов.");
  }
}
