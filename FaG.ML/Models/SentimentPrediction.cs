using FaG.Data.DAL;
using Microsoft.ML.Data;

namespace FaG.ML.Models
{
  /// <summary>
  /// Output model for sentiment prediction results
  /// </summary>
  public class SentimentPrediction
  {
    /// <summary>
    /// Original input text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Predicted sentiment class (0=Negative, 1=Neutral, 2=Positive)
    /// </summary>
    [ColumnName("PredictedLabel")]
    public uint PredictedLabel { get; set; }

    /// <summary>
    /// Confidence scores for each class [Negative, Neutral, Positive]
    /// </summary>
    [ColumnName("Score")]
    public float[] Score { get; set; } = [];

    /// <summary>
    /// Predicted emotion based on label
    /// </summary>
    public Emotion Emotion => (Emotion)PredictedLabel;

    /// <summary>
    /// Confidence score for the predicted emotion (0-1)
    /// </summary>
    public float Confidence => Score.Length > 0 ? Score[(int)PredictedLabel] : 0f;

    /// <summary>
    /// Detailed confidence scores
    /// </summary>
    public SentimentScores SentimentScores => new SentimentScores
    {
      NegativeScore = Score.Length > 0 ? Score[0] : 0f,
      NeutralScore = Score.Length > 1 ? Score[1] : 0f,
      PositiveScore = Score.Length > 2 ? Score[2] : 0f
    };
  }

  /// <summary>
  /// Detailed sentiment scores for each emotion
  /// </summary>
  public class SentimentScores
  {
    public float NegativeScore { get; set; }
    public float NeutralScore { get; set; }
    public float PositiveScore { get; set; }
  }
}
