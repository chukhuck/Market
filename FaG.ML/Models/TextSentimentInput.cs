using Microsoft.ML.Data;

namespace FaG.ML.Models
{
  /// <summary>
  /// Input model for loading financial news sentiment data from TSV file
  /// </summary>
  public class TextSentimentInput
  {
    /// <summary>
    /// Column 1: Sentiment score from -1 (negative) to 1 (positive)
    /// </summary>
    [LoadColumn(1)]
    public float Sentiment_Score { get; set; }

    /// <summary>
    /// Column 3: Article summary/text content - used for sentiment analysis
    /// </summary>
    [LoadColumn(2)]
    public string Body { get; set; } = string.Empty;
  }
}