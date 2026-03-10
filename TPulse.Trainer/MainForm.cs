using System.Text.Json;
using TPulse.Data;
using TPulse.Client;
using TPulse.Client.Model;
using TPulse.Trainer.DAL;

public partial class MainForm : Form
{
  private readonly TPulseApiClient _pulseClient;
  private const int limit = 10;
  private bool hasNext;
  private List<Post> _posts;
  private int _currentPostIndex;
  private string? nextCursor;
  private readonly AppDbContext _dbContext;

  private const string BroadcastUrl = "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed?appName=invest&origin=web&platform=web&include=all";
  private const string SearchUrl = "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}?appName=invest&origin=web&platform=web&include=all";
  private const string ImageUrl = "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg";
  private const string defaultNextCursor = "1772561236013000:OLD:0303232614";

  // Конфиг рядом с exe (не в AppData)
  private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

  public MainForm()
  {
    InitializeComponent();

    // улучшение отображения emoji: попытка использовать шрифт с поддержкой emoji
    try
    {
      var preferred = "Segoe UI Emoji";
      var fallback = "Segoe UI Symbol";
      var fontName = FontFamily.Families.Any(f => f.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase))
        ? preferred
        : (FontFamily.Families.Any(f => f.Name.Equals(fallback, StringComparison.OrdinalIgnoreCase)) ? fallback : postTextTextBox.Font.FontFamily.Name);

      postTextTextBox.Font = new Font(fontName, postTextTextBox.Font.Size, postTextTextBox.Font.Style);
    }
    catch
    {
      // игнорируем — используем системный шрифт по умолчанию
    }

    _pulseClient = new TPulseApiClient(BroadcastUrl, SearchUrl, ImageUrl);
    _dbContext = new AppDbContext();
    _posts = [];
    _currentPostIndex = -1;
    nextCursor = LoadNextCursorFromConfig();
    hasNext = true;

    txtCurrentCursor.Text = nextCursor ?? defaultNextCursor;
  }

  private string LoadNextCursorFromConfig()
  {
    try
    {
      if (!File.Exists(_configPath))
        return defaultNextCursor;

      var json = File.ReadAllText(_configPath);
      if (string.IsNullOrWhiteSpace(json))
        return defaultNextCursor;

      using var doc = JsonDocument.Parse(json);
      if (doc.RootElement.TryGetProperty("NextCursor", out var el) && el.ValueKind == JsonValueKind.String)
        return el.GetString() ?? defaultNextCursor;
    }
    catch
    {
    }

    return defaultNextCursor;
  }

  private static JsonSerializerOptions GetOptions()
  {
    return new JsonSerializerOptions { WriteIndented = true };
  }

  private void SaveConfig(JsonSerializerOptions serializeOptions)
  {
    try
    {
      var dir = Path.GetDirectoryName(_configPath);
      if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        Directory.CreateDirectory(dir);

      var obj = new { NextCursor = nextCursor ?? defaultNextCursor };
      var json = JsonSerializer.Serialize(obj, serializeOptions);
      File.WriteAllText(_configPath, json);
    }
    catch
    {
    }
  }
  protected override void OnFormClosing(FormClosingEventArgs e)
  {
    SaveConfig(GetOptions());
    base.OnFormClosing(e);
  }
  private async Task LoadNextPostsAsync()
  {
    try
    {
      if (!hasNext)
      {
        MessageBox.Show("Нет новых постов для загрузки.");
        return;
      }

      var response = await _pulseClient.GetPostsAsync(count: limit, cursor: nextCursor);
      await HandlePayload(response);
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Ошибка загрузки постов: {ex.Message}");
    }
  }
  private async Task DisplayCurrentPostAsync()
  {
    if (_currentPostIndex < 0 || _currentPostIndex >= _posts.Count)
      return;

    var post = _posts[_currentPostIndex];

    postTextTextBox.Text = post?.Content?.Text ?? "Отсутствует текст поста";

    if (post != null)
    {


      try
      {
        txtPostInserted.Text = post.Inserted.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
      }
      catch
      {
        txtPostInserted.Text = post.Inserted.ToString();
      }
    }
    else
    {
      txtPostInserted.Text = string.Empty;
    }


    if (post is null || post.Content is null || post.Content.Images is null || post.Content.Images.Count == 0)
    {
      postImagePictureBox.Image = null;
      return;
    }

    await LoadImageAsync(post.Content.Images.First().Id);
  }
  private async Task LoadImageAsync(Guid imageId)
  {
    try
    {
      using var stream = new MemoryStream(await _pulseClient.GetImageAsync(imageId).ConfigureAwait(false));
      postImagePictureBox.Image = System.Drawing.Image.FromStream(stream);
    }
    catch
    {
      postImagePictureBox.Image = null;
    }
  }
  private async Task SaveEvaluationAsync(Emotion emotion)
  {
    if (_currentPostIndex < 0 || _currentPostIndex >= _posts.Count)
      return;

    var post = _posts[_currentPostIndex];

    if (post == null)
    {
      MessageBox.Show("Ошибка: текущий пост недоступен для оценки.");
      await GoToNextPostAsync();
      return;
    }

    var evaluation = post.ToPostEvaluation(emotion);

    await _dbContext.UserPostEvaluations.AddAsync(evaluation);
    await _dbContext.SaveChangesAsync();
    await GoToNextPostAsync();
  }
  private async Task GoToNextPostAsync()
  {
    _currentPostIndex++;

    if (_currentPostIndex >= _posts.Count)
    {
      var ticker = txtTickerFilter.Text.Trim();
      if (ticker != "")
      {
        await LoadNextPostByTickerAsync(ticker);
      }
      else
      {
        await LoadNextPostsAsync();
      }

    }
    else
    {
      await DisplayCurrentPostAsync();
    }
  }
  private async Task<bool> HandlePayload(PostsResponse response)
  {
    var payload = response?.Payload;

    if (payload == null || payload.Items == null || payload.Items.Count == 0)
    {
      MessageBox.Show("Нет новых постов для загрузки.");
      return false;
    }

    hasNext = payload.HasNext;
    nextCursor = payload.NextCursor ?? defaultNextCursor;
    txtCurrentCursor.Text = nextCursor;

    _posts = [.. payload.Items.Where(i => !string.IsNullOrEmpty(i.Content?.Text))];
    _currentPostIndex = 0;

    await DisplayCurrentPostAsync();
    return true;
  }
  protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
  {
    switch (keyData)
    {
      case Keys.Left:
        _ = SaveEvaluationAsync(Emotion.Negative);
        return true;
      case Keys.Right:
        _ = SaveEvaluationAsync(Emotion.Positive);
        return true;
      case Keys.Up:
        _ = SaveEvaluationAsync(Emotion.Neutral);
        return true;
      case Keys.Down:
        _ = SaveEvaluationAsync(Emotion.Skipped);
        return true;
    }
    return base.ProcessCmdKey(ref msg, keyData);
  }
  private async void MainForm_Load(object sender, EventArgs e)
  {
    await LoadNextPostsAsync();
  }
  private async void TxtTickerFilter_TextChanged(object sender, EventArgs e)
  {
    var ticker = txtTickerFilter.Text.Trim();
    if (!string.IsNullOrEmpty(ticker))
    {
      await LoadNextPostByTickerAsync(ticker);
    }
    else
    {
      await LoadNextPostsAsync();
    }
  }

  private async Task LoadNextPostByTickerAsync(string ticker)
  {
    if (!hasNext)
    {
      MessageBox.Show("Нет новых постов для загрузки.");
      return;
    }

    try
    {
      var response = await _pulseClient.SearchPostsByTickerAsync(ticker, count: limit, cursor: nextCursor);
      await HandlePayload(response);
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Ошибка поиска по тикеру: {ex.Message}");
    }
  }

  private async void BtnPositive_Click(object sender, EventArgs e) => await SaveEvaluationAsync(Emotion.Positive);
  private async void BtnNegative_Click(object sender, EventArgs e) => await SaveEvaluationAsync(Emotion.Negative);
  private async void BtnNeutral_Click(object sender, EventArgs e) => await SaveEvaluationAsync(Emotion.Neutral);
  private async void BtnSkip_Click(object sender, EventArgs e) => await SaveEvaluationAsync(Emotion.Skipped);
}
