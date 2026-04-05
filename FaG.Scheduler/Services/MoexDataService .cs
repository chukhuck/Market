using FaG.Data.DAL;
using System.Text.Json;

namespace FaG.Scheduler.Services
{
  public class MoexResponse
  {
    public HistoryData History { get; set; }
  }

  public class HistoryData
  {
    public List<string> Columns { get; set; }
    public List<List<object>> Data { get; set; }
  }

  public class MoexDataService : IMoexDataService
  {
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://iss.moex.com/iss/history/engines/stock/markets/index/boards/SNDX/securities/IMOEX.json";

    public MoexDataService(HttpClient httpClient)
    {
      _httpClient = httpClient;
      // Устанавливаем таймаут для запросов
      _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<IMOEXIndexTradeDay>> GetIMOEXDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken token)
    {
      var from = startDate.ToString("yyyy-MM-dd");
      var till = endDate.ToString("yyyy-MM-dd");

      var url = $"{BaseUrl}?from={from}&till={till}";

      try
      {
        var response = await _httpClient.GetAsync(url, token);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(token);
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        var moexResponse = JsonSerializer.Deserialize<MoexResponse>(
            content,
            options);

        if (moexResponse == null)
        {
          Console.WriteLine("Received empty response from MOEX API.");
          return [];
        }

        return ParseMoexData(moexResponse);
      }
      catch (HttpRequestException ex)
      {
        Console.WriteLine($"HTTP error loading IMOEX data: {ex.Message}");
        throw;
      }
      catch (JsonException ex)
      {
        Console.WriteLine($"JSON parsing error: {ex.Message}");
        throw;
      }
    }

    private static List<IMOEXIndexTradeDay> ParseMoexData(MoexResponse response)
    {
      if (response?.History?.Data == null || response.History.Columns == null)
        return [];

      var columnMap = response.History.Columns
          .Select((name, index) => new { name, index })
          .ToDictionary(x => x.name.ToLowerInvariant(), x => x.index);

      return [.. response.History.Data.Select(row =>
      {
        object? getValue(string columnName)
        {
          return columnMap.TryGetValue(columnName, out int value) && row.Count > value ? row[value]
            : null;
        }

        return new IMOEXIndexTradeDay
        {
          Date = DateTime.Parse(getValue("tradedate")?.ToString() ?? ""),
          Open = Convert.ToDouble(getValue("open") ?? 0),
          High = Convert.ToDouble(getValue("high") ?? 0),
          Low = Convert.ToDouble(getValue("low") ?? 0),
          Close = Convert.ToDouble(getValue("close") ?? 0),
          Volume = Convert.ToDouble(getValue("volume") ?? 0)
        };
      })];
    }
  }
}
