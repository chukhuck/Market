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
    /// Predicted sentiment score from -1 (negative) to 1 (positive)
    /// </summary>
    public float PredictedScore { get; set; }

    /// <summary>
    /// Predicted emotion based on score
    /// </summary>
    public Emotion Emotion { get; set; }

    /// <summary>
    /// Confidence as absolute distance from decision boundary
    /// Closer to -1 or 1 = more confident, closer to 0 = less confident
    /// </summary>
    public float Confidence => Math.Abs(PredictedScore) > Math.Abs(0f) ? Math.Abs(PredictedScore) : 0f;

    /// <summary>
    /// Detailed confidence scores for each emotion
    /// </summary>
    public SentimentScores SentimentScores => CalculateSentimentScores();

    private SentimentScores CalculateSentimentScores()
    {
      // Map continuous score [-1, 1] to probabilities
      // Using softmax-like distribution centered at boundaries
      float negativeScore = 0f;
      float neutralScore = 0f;
      float positiveScore = 0f;

      if (PredictedScore < -0.33f)
      {
        negativeScore = Math.Min(1f, (-PredictedScore) / 1f); // Closer to -1 = more confident
        neutralScore = Math.Max(0f, 1f - Math.Abs(PredictedScore + 0.33f) / 0.67f);
      }
      else if (PredictedScore <= 0.33f && PredictedScore >= -0.33f)
      {
        neutralScore = 1f - (Math.Abs(PredictedScore) / 0.33f) * 0.5f;
        negativeScore = Math.Max(0f, (-PredictedScore) / 0.33f * 0.5f);
        positiveScore = Math.Max(0f, PredictedScore / 0.33f * 0.5f);
      }
      else
      {
        positiveScore = Math.Min(1f, PredictedScore / 1f); // Closer to 1 = more confident
        neutralScore = Math.Max(0f, 1f - Math.Abs(PredictedScore - 0.33f) / 0.67f);
      }

      return new SentimentScores
      {
        NegativeScore = negativeScore,
        NeutralScore = neutralScore,
        PositiveScore = positiveScore
      };
    }
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

