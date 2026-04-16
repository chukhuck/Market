using FaG.ML;
using FaG.ML.Models;

namespace FaG.ML.Training
{
  /// <summary>
  /// Training orchestrator for sentiment model development
  /// </summary>
  public class SentimentTrainer
  {
    private readonly SentimentModelBuilder _modelBuilder;
    private readonly string _outputModelPath;

    public SentimentTrainer(string outputModelPath = "./Models/SentimentModel.zip")
    {
      _modelBuilder = new SentimentModelBuilder();
      _outputModelPath = outputModelPath;
    }

    /// <summary>
    /// Trains the sentiment model from the TSV dataset
    /// </summary>
    /// <param name="datasetPath">Path to the webofrussia.tsv file</param>
    public void TrainModel(string datasetPath)
    {
      if (!File.Exists(datasetPath))
      {
        throw new FileNotFoundException($"Dataset not found at: {datasetPath}");
      }

      Console.WriteLine("🚀 Starting sentiment model training...");
      Console.WriteLine($"📁 Dataset: {datasetPath}");

      try
      {
        // Train the model
        var startTime = DateTime.UtcNow;
        Console.WriteLine("⏳ Building and training pipeline...");

        var model = _modelBuilder.BuildAndTrain(datasetPath);

        var trainingDuration = DateTime.UtcNow - startTime;
        Console.WriteLine($"✅ Training completed in {trainingDuration.TotalSeconds:F2}s");

        // Save the model
        Console.WriteLine($"💾 Saving model to {_outputModelPath}...");
        _modelBuilder.SaveModel(_outputModelPath);
        Console.WriteLine($"✅ Model saved successfully");

        // Display sample predictions
        Console.WriteLine("\n📊 Sample Predictions:");
        Console.WriteLine(new string('-', 80));
        DisplaySamplePredictions();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"❌ Training failed: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Demonstrates model predictions on sample texts
    /// </summary>
    private void DisplaySamplePredictions()
    {
      var sampleTexts = new[]
      {
                "Компания показала рекордный рост прибыли и расширяет рынки сбыта",
                "Производство сокращено на 30% из-за проблем в цепи поставок",
                "Акции компании торгуются стабильно без значительных изменений",
                "Убытки компании увеличились вдвое на фоне кризиса",
                "Новый продукт получил высокие оценки от аналитиков",
                "Яндекс увеличил прибыль на $1 млрд"
            };

      foreach (var text in sampleTexts)
      {
        try
        {
          var prediction = _modelBuilder.Predict(text);

          Console.WriteLine($"\n📝 Text: {text[..Math.Min(60, text.Length)]}...");
          Console.WriteLine($"🎯 Prediction: {prediction.Emotion}");
          Console.WriteLine($"📈 Score: {prediction.Score}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"⚠️  Prediction failed: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Loads a trained model for inference
    /// </summary>
    public void LoadTrainedModel()
    {
      if (!File.Exists(_outputModelPath))
      {
        throw new FileNotFoundException($"Model not found at: {_outputModelPath}");
      }

      Console.WriteLine($"📂 Loading model from {_outputModelPath}...");
      _modelBuilder.LoadModel(_outputModelPath);
      Console.WriteLine("✅ Model loaded successfully");
    }

    /// <summary>
    /// Gets the model builder for direct access
    /// </summary>
    public SentimentModelBuilder GetModelBuilder() => _modelBuilder;
  }
}
