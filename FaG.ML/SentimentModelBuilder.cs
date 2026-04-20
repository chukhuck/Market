using FaG.Data.DAL;
using FaG.ML.Models;
using FaG.ML.Utilities;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Transforms.Text;
using static TorchSharp.torch.utils;

namespace FaG.ML
{
  public class SentimentPrediction
  {
    public float Score { get; set; }

    public string? Text { get; set; }

    public Emotion Emotion { get; set; }
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
      var trainData = _mlContext.Data.LoadFromTextFile<TextSentimentInput>(
            dataPath,
            hasHeader: true,
            separatorChar: '\t',
            allowQuoting: true,
            trimWhitespace: true);

      // Build ML pipeline
      var pipeline = BuildPipeline();

      // Train the model
      _model = pipeline.Fit(trainData);

      return _model;
    }

    /// <summary>
    /// Builds the ML.NET pipeline for text sentiment regression
    /// </summary>
    private IEstimator<ITransformer> BuildPipeline()
    {
      //var tokenizer = new RuBertTokenizer("./Data/onnx/rubert-tiny2-onnx/vocab.txt");

      return _mlContext.Transforms
          .Text.NormalizeText(
              outputColumnName: "NormalizedText",
              inputColumnName: nameof(TextSentimentInput.Body),
              caseMode: TextNormalizingEstimator.CaseMode.Lower)
          .Append(_mlContext.Transforms.CustomMapping<TextSentimentInput, BertTokenOutput>(
            mapAction: TokenizeMappings.TokenizeMapping, 
            contractName: nameof(TokenizeMappings.TokenizeMapping)))
          //.Append(_mlContext.Transforms.CustomMapping<TextSentimentInput, BertTokenOutput>(
          //    mapAction: (input, output) =>
          //    {
          //      (output.InputIds, output.AttentionMask) = tokenizer.Tokenize(input.Body);
          //    },
          //    contractName: "RuBertTokenizer"))
          .Append(_mlContext.Transforms.ApplyOnnxModel(
              modelFile: "Data/onnx/rubert-tiny2-onnx/model.onnx",
              outputColumnNames: new[] { "tanh" },
              inputColumnNames: new[] { "input_ids", "attention_mask" }))
          .Append(_mlContext.Transforms.Conversion.ConvertType(
              outputColumnName: "Features",
              inputColumnName: "tanh",
              DataKind.Single))
          .Append(_mlContext.Transforms.NormalizeMinMax(
              outputColumnName: "Features",
              inputColumnName: "Features"))
          .Append(_mlContext.Regression.Trainers.LightGbm(
              labelColumnName: nameof(TextSentimentInput.Sentiment_Score),
              featureColumnName: "Features"));

    }

    /// <summary>
    /// Evaluates model performance on test data
    /// </summary>
    public RegressionMetrics Evaluate(string testDataPath)
    {
      if (_model == null)
        throw new InvalidOperationException("Model has not been trained. Call BuildAndTrain first.");

      var rawTestData = _mlContext.Data.LoadFromTextFile<TextSentimentInput>(
      testDataPath,
      hasHeader: true,
      separatorChar: '\t',
      allowQuoting: true);

      var predictions = _model.Transform(rawTestData);

      var metrics = _mlContext.Regression.Evaluate(
          predictions,
          labelColumnName: nameof(TextSentimentInput.Sentiment_Score),
          scoreColumnName: "Score");

      return metrics;
    }

    /// <summary>
    /// Makes sentiment prediction for a given text
    /// </summary>
    public SentimentPrediction Predict(string text)
    {
      if (_model == null)
        throw new InvalidOperationException("Model has not been trained. Call BuildAndTrain first.");

      var predictionEngine = _mlContext.Model.CreatePredictionEngine<TextSentimentInput, TextSentimentOutput>(_model);

      var sample = new TextSentimentInput { Body = text };
      var prediction = predictionEngine.Predict(sample);

      // Clamp the predicted score to [-1, 1] range
      var clampedScore = Math.Clamp(prediction.Score, -1f, 1f);

      return new SentimentPrediction
      {
        Text = text,
        Score = clampedScore,
        Emotion = ConvertScoreToEmotion(clampedScore)
      };
    }

    /// <summary>
    /// Converts continuous score to discrete emotion class
    /// </summary>
    private Emotion ConvertScoreToEmotion(float score)
    {
      return score switch
      {
        < -0.25f => Emotion.Negative,
        <= 0.25f and >= -0.25f => Emotion.Neutral,
        _ => Emotion.Positive
      };
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

      _mlContext.ComponentCatalog.RegisterAssembly(typeof(TokenizeMappings).Assembly);
      _model = _mlContext.Model.Load(modelPath, out _);
    }

    /// <summary>
    /// Gets the current model transformer
    /// </summary>
    public ITransformer? GetModel() => _model;
  }
}