using System.Net;
using System.Text;
using System.Text.Json;
using TPulse.Client.Model;

namespace TPulse.Client
{
  public class TPulseApiClient
  {
    private readonly HttpClient _httpClient;
    private readonly string _broadcastUrl;
    private readonly string _searchUrl;
    private readonly string _imageUrl;


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true
    };

    public TPulseApiClient(string broadcastUrl, string searchUrl, string imageUrl)
    {
      var handler = new HttpClientHandler
      {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
      };

      _httpClient = new HttpClient(handler)
      {
        Timeout = TimeSpan.FromSeconds(30)
      };

      _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.114 Safari/537.36");
      _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
      _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
      _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
      _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "utf-8");

      _broadcastUrl = broadcastUrl;
      _searchUrl = searchUrl;
      _imageUrl = imageUrl;
    }
    //https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed?appName=invest&origin=web&platform=web&limit=2&cursor={cursor}
    public async Task<PostsResponse> GetPostsAsync(int count, string? cursor)
    {
      try
      {
        if(count <= 0)
          count = 20; // Устанавливаем значение по умолчанию, если передано некорректное

        var url =  string.IsNullOrEmpty(cursor) ? $"{_broadcastUrl}&limit={count}" : $"{_broadcastUrl}&limit={count}&cursor={cursor}";
        var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
          throw new HttpRequestException($"API вернул ошибку: {response.StatusCode}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        Encoding encoding = Encoding.UTF8;
        try
        {
          var charset = response.Content.Headers.ContentType?.CharSet;
          if (!string.IsNullOrWhiteSpace(charset))
            encoding = Encoding.GetEncoding(charset.Trim('"'));
        }
        catch
        {
          encoding = Encoding.UTF8;
        }

        var json = encoding.GetString(bytes);
        return JsonSerializer.Deserialize<PostsResponse>(json, JsonOptions) ?? new PostsResponse();
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка загрузки постов: {ex.Message}", ex);
      }
    }
    public async Task<PostsResponse> SearchPostsByTickerAsync(string? ticker, int count, string? cursor)
    {
      if (string.IsNullOrWhiteSpace(ticker))
        throw new ArgumentException("Тикер не может быть пустым.", nameof(ticker));

      if (count <= 0)
        count = 20; // Устанавливаем значение по умолчанию, если передано некорректное

      var url = $"{_searchUrl}&limit={count}";

      if (!string.IsNullOrEmpty(cursor) && !cursor.Contains("OLD"))
      {
        url += $"&cursor={cursor}";
      }

      url = url.Replace("{tiker}", ticker);

      try
      {
        var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
          throw new HttpRequestException($"API вернул ошибку: {response.StatusCode}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        Encoding encoding = Encoding.UTF8;
        try
        {
          var charset = response.Content.Headers.ContentType?.CharSet;
          if (!string.IsNullOrWhiteSpace(charset))
            encoding = Encoding.GetEncoding(charset.Trim('"'));
        }
        catch
        {
          encoding = Encoding.UTF8;
        }

        var json = encoding.GetString(bytes);
        return JsonSerializer.Deserialize<PostsResponse>(json, JsonOptions) ?? new PostsResponse();
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка загрузки постов: {ex.Message}", ex);
      }
    }
    public string GetImageUrl(Guid imageId)
    {
      return _imageUrl.Replace("{guid_image}", imageId.ToString());
    }
    public async Task<byte[]> GetImageAsync(Guid imageId)
    {
      try
      {
        var imageUrl = GetImageUrl(imageId);
        // Используем тот же _httpClient (с поддержкой декомпрессии)
        return await _httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка загрузки картинки: {ex.Message}", ex);
      }
    }
  }
}
