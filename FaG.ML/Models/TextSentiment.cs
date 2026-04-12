using Microsoft.ML.Data;
using System.Runtime.Serialization;

namespace FaG.ML.Models
{
  /// <summary>
  /// Input model for loading financial news sentiment data from TSV file
  /// </summary>
  public class TextSentiment
  {
    /// <summary>
    /// Column 0: Article title
    /// </summary>
    [LoadColumn(0)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Column 1: Sentiment score from -1 (negative) to 1 (positive)
    /// </summary>
    [LoadColumn(1)]
    public float Score { get; set; }

    /// <summary>
    /// Column 2: URL link to the article
    /// </summary>
    [LoadColumn(2)]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Column 3: Article summary/text content - used for sentiment analysis
    /// </summary>
    [LoadColumn(4)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Column 4: Publication date
    /// </summary>
    [LoadColumn(5)]
    public string Published { get; set; } = string.Empty;

    /// <summary>
    /// Column 5: Stock tickers mentioned
    /// </summary>
    [LoadColumn(6)]
    public string Tickers { get; set; } = string.Empty;

    /// <summary>
    /// Computed label for classification (0=Negative, 1=Neutral, 2=Positive)
    /// </summary>
    public uint Label { get; set; }

    /// <summary>
    /// Converts continuous sentiment score to discrete emotion class
    /// </summary>
    public void ComputeLabel()
    {
      Label = Score switch
      {
        < -0.33f => 0,  // Negative
        <= 0.33f and >= -0.33f => 1,  // Neutral
        _ => 2           // Positive
      };
    }
  }
}