using FaG.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

namespace FaG.ML
{
  public class PredictionResultFull
  {
    [ColumnName("Label")]
    public uint Label { get; set; }

    [ColumnName("PredictedLabel")]
    public uint PredictedLabel { get; set; }
  }

  /// <summary>
  /// Builder for training and using sentiment analysis models
  /// </summary>
  public class SentimentModelBuilder
  {
    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public SentimentModelBuilder()
    {
      _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Builds and trains a multi-class sentiment classification model
    /// </summary>
    /// <param name="dataPath">Path to the TSV training data file</param>
    /// <returns>Trained model transformer</returns>
    public ITransformer BuildAndTrain(string dataPath)
    {
      var rawData = _mlContext.Data.LoadFromTextFile<TextSentimentRaw>(
            dataPath,
            hasHeader: true,
            separatorChar: '\t',
            allowQuoting: true);

      var transformedData = _mlContext.Data.CreateEnumerable<TextSentimentRaw>(rawData, reuseRowObject: false)
          .Select(x => new TextSentiment
          {
            Title = x.Title,
            Score = x.Score,
            Link = x.Link,
            Summary = x.Summary,
            Published = x.Published,
            Tickers = x.Tickers
          })
          .ToList();

      foreach (var item in transformedData)
      {
        item.ComputeLabel();
      }

      var trainData = _mlContext.Data.LoadFromEnumerable(transformedData);

      // Build ML pipeline
      var pipeline = BuildPipeline();

      // Train the model
      _model = pipeline.Fit(trainData);

      return _model;
    }

    /// <summary>
    /// Builds the ML.NET pipeline for text sentiment analysis
    /// </summary>
    private IEstimator<ITransformer> BuildPipeline()
    {
      return _mlContext.Transforms
          .Text.NormalizeText(outputColumnName: "NormalizedText", inputColumnName: nameof(TextSentiment.Summary))
          .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
          .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "NormalizedText"))
          .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(TextSentiment.Label)))
          //.Append(_mlContext.Transforms.Conversion.ConvertType("LabelFloat", nameof(TextSentiment.Label), DataKind.Single))
          .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
              labelColumnName: "Label",
              featureColumnName: "Features"))
          .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
    }

    /// <summary>
    /// Evaluates model performance on test data
    /// </summary>
    public MulticlassClassificationMetrics Evaluate(string testDataPath)
    {
      if (_model == null)
        throw new InvalidOperationException("Model has not been trained. Call BuildAndTrain first.");

      var rawTestData = _mlContext.Data.LoadFromTextFile<TextSentimentRaw>(
      testDataPath,
      hasHeader: true,
      separatorChar: '\t',
      allowQuoting: true);

      var testTransformed = _mlContext.Data.CreateEnumerable<TextSentimentRaw>(rawTestData, reuseRowObject: false)
          .Select(x => new TextSentiment
          {
            Title = x.Title,
            Score = x.Score,
            Link = x.Link,
            Summary = x.Summary,
            Published = x.Published,
            Tickers = x.Tickers
          })
          .ToList();

      foreach (var item in testTransformed)
      {
        item.ComputeLabel();
      }

      var testDataTransformed = _mlContext.Data.LoadFromEnumerable(testTransformed);
      var predictions = _model.Transform(testDataTransformed);

      var results = _mlContext.Data.CreateEnumerable<PredictionResultFull>(predictions, reuseRowObject: false);
      foreach (var result in results)
      {
        Console.WriteLine($"Label: {result.Label}, Predicted: {result.PredictedLabel}");
      }


      var metrics = _mlContext.MulticlassClassification.Evaluate(
          predictions,
          labelColumnName: "Label",
          predictedLabelColumnName: "PredictedLabel");

      return metrics;
    }

    /// <summary>
    /// Makes sentiment prediction for a given text
    /// </summary>
    public SentimentPrediction Predict(string text)
    {
      if (_model == null)
        throw new InvalidOperationException("Model has not been trained. Call BuildAndTrain first.");

      var predictionEngine = _mlContext.Model.CreatePredictionEngine<TextSentiment, SentimentPrediction>(_model);

      var sample = new TextSentiment { Summary = text };
      var prediction = predictionEngine.Predict(sample);
      prediction.Text = text;

      return prediction;
    }

    /// <summary>
    /// Batch prediction for multiple texts
    /// </summary>
    public IEnumerable<SentimentPrediction> PredictBatch(IEnumerable<string> texts)
    {
      var predictions = new List<SentimentPrediction>();

      foreach (var text in texts)
      {
        predictions.Add(Predict(text));
      }

      return predictions;
    }

    /// <summary>
    /// Saves the trained model to a file
    /// </summary>
    public void SaveModel(string modelPath)
    {
      if (_model == null)
        throw new InvalidOperationException("Model has not been trained. Call BuildAndTrain first.");

      // Ensure directory exists
      var directory = Path.GetDirectoryName(modelPath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      _mlContext.Model.Save(_model, null, modelPath);
    }

    /// <summary>
    /// Loads a previously trained model from a file
    /// </summary>
    public void LoadModel(string modelPath)
    {
      if (!File.Exists(modelPath))
        throw new FileNotFoundException($"Model file not found: {modelPath}");

      _model = _mlContext.Model.Load(modelPath, out _);
    }

    /// <summary>
    /// Gets the current model transformer
    /// </summary>
    public ITransformer? GetModel() => _model;
  }

  /// <summary>
  /// Helper class for model evaluation results
  /// </summary>
  public class ModelEvaluationReport
  {
    public double MacroAccuracy { get; set; }
    public double MicroAccuracy { get; set; }
    public double LogLoss { get; set; }
    public double LogLossReduction { get; set; }
    public ConfusionMatrix ConfusionMatrix { get; set; } = new();

    public override string ToString()
    {
      return $@"
=== Model Evaluation Report ===
Macro Accuracy: {MacroAccuracy:P4}
Micro Accuracy: {MicroAccuracy:P4}
Log Loss: {LogLoss:F4}
Log Loss Reduction: {LogLossReduction:F4}

Confusion Matrix:
{ConfusionMatrix}
";
    }
  }

  /// <summary>
  /// Confusion matrix for multi-class classification
  /// </summary>
  public class ConfusionMatrix
  {
    private int[,]? _matrix;

    public void SetMatrix(int[,] matrix)
    {
      _matrix = matrix;
    }

    public override string ToString()
    {
      if (_matrix == null)
        return "Matrix not set";

      var sb = new System.Text.StringBuilder();
      var labels = new[] { "Negative", "Neutral", "Positive" };

      sb.AppendLine("       Predicted Negative  Neutral  Positive");
      for (int i = 0; i < _matrix.GetLength(0); i++)
      {
        sb.Append($"{labels[i],8}");
        for (int j = 0; j < _matrix.GetLength(1); j++)
        {
          sb.Append($"{_matrix[i, j],9}");
        }
        sb.AppendLine();
      }

      return sb.ToString();
    }
  }
}