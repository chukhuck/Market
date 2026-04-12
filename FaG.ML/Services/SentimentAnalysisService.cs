using FaG.Data.DAL;
using FaG.ML.Models;

namespace FaG.ML.Services
{
  /// <summary>
  /// Service for sentiment analysis using the trained ML model
  /// </summary>
  public interface ISentimentAnalysisService
  {
    /// <summary>
    /// Analyzes sentiment of a single text
    /// </summary>
    Task<SentimentPrediction> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes sentiment of multiple texts in batch
    /// </summary>
    Task<IEnumerable<SentimentPrediction>> AnalyzeSentimentBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the model is loaded and ready
    /// </summary>
    bool IsModelLoaded { get; }
  }

  /// <summary>
  /// Implementation of sentiment analysis service
  /// </summary>
  public class SentimentAnalysisService : ISentimentAnalysisService
  {
    private readonly SentimentModelBuilder _modelBuilder;
    private bool _isModelLoaded;

    public bool IsModelLoaded => _isModelLoaded;

    public SentimentAnalysisService(SentimentModelBuilder modelBuilder)
    {
      _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
    }

    /// <summary>
    /// Loads the model from file
    /// </summary>
    public void InitializeModel(string modelPath)
    {
      if (!File.Exists(modelPath))
        throw new FileNotFoundException($"Model file not found: {modelPath}");

      _modelBuilder.LoadModel(modelPath);
      _isModelLoaded = true;
    }

    public Task<SentimentPrediction> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
      if (!_isModelLoaded)
        throw new InvalidOperationException("Model is not loaded. Call InitializeModel first.");

      if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException("Text cannot be null or empty.", nameof(text));

      try
      {
        var prediction = _modelBuilder.Predict(text);
        return Task.FromResult(prediction);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Sentiment analysis failed: {ex.Message}", ex);
      }
    }

    public Task<IEnumerable<SentimentPrediction>> AnalyzeSentimentBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
      if (!_isModelLoaded)
        throw new InvalidOperationException("Model is not loaded. Call InitializeModel first.");

      if (texts == null)
        throw new ArgumentNullException(nameof(texts));

      try
      {
        var predictions = _modelBuilder.PredictBatch(texts).ToList();
        return Task.FromResult((IEnumerable<SentimentPrediction>)predictions);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Batch sentiment analysis failed: {ex.Message}", ex);
      }
    }
  }

  /// <summary>
  /// Extension methods for sentiment analysis
  /// </summary>
  public static class SentimentAnalysisExtensions
  {
    /// <summary>
    /// Gets human-readable emotion description
    /// </summary>
    public static string GetEmotionDescription(this Emotion emotion) => emotion switch
    {
      Emotion.Negative => "Отрицательная тональность",
      Emotion.Neutral => "Нейтральная тональность",
      Emotion.Positive => "Положительная тональность",
      Emotion.None => "Неизвестная тональность",
      _ => "Неопределённая тональность"
    };

    /// <summary>
    /// Gets emoji representation of emotion
    /// </summary>
    public static string GetEmotionEmoji(this Emotion emotion) => emotion switch
    {
      Emotion.Negative => "😞",
      Emotion.Neutral => "😐",
      Emotion.Positive => "😊",
      Emotion.None => "❓",
      _ => "❓"
    };

    /// <summary>
    /// Gets color representation for UI
    /// </summary>
    public static string GetEmotionColor(this Emotion emotion) => emotion switch
    {
      Emotion.Negative => "#dc3545", // Red
      Emotion.Neutral => "#6c757d",  // Gray
      Emotion.Positive => "#28a745", // Green
      Emotion.None => "#999999",     // Dark gray
      _ => "#999999"
    };

    /// <summary>
    /// Formats sentiment prediction for display
    /// </summary>
    public static string FormatPrediction(this SentimentPrediction prediction) =>
        $"{prediction.Emotion.GetEmotionEmoji()} {prediction.Emotion.GetEmotionDescription()} (уверенность: {prediction.Confidence:P1})";
  }
}
