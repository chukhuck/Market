using Microsoft.ML.Data;

namespace FaG.ML.Models
{
  public class TextSentimentRaw
  {
    [LoadColumn(0)]
    public string Title { get; set; } = string.Empty;

    [LoadColumn(1)]
    public float Score { get; set; }

    [LoadColumn(2)]
    public string Link { get; set; } = string.Empty;

    [LoadColumn(4)]
    public string Summary { get; set; } = string.Empty;

    [LoadColumn(5)]
    public string Published { get; set; } = string.Empty;

    [LoadColumn(6)]
    public string Tickers { get; set; } = string.Empty;
  }
}
