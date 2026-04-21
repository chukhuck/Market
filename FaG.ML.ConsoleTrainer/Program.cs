using FaG.ML;
using FaG.ML.Services;
using FaG.ML.Training;
using FaG.ML.Utilities;
using System.Text;

var datasetPath = "Data/RussianFinancialNews_ya_balance_body.tsv";
var trainPath = "Data/train.tsv";
var testPath = "Data/test.tsv";
var modelPath = "Models/SentimentModelYaBalanced_rubert_LightGbm.zip";

//// 1. Анализ исходного датасета
//Console.WriteLine("📊 Анализ датасета:");
//var stats = DatasetUtilities.AnalyzeDataset(datasetPath);
//Console.WriteLine(stats);
//
//if (DatasetUtilities.ValidateDataset(datasetPath, out var errors))
//{
//  Console.WriteLine("✅ Датасет валиден");
//}
//else
//{
//  errors.ForEach(e => Console.WriteLine($"❌ {e}"));
//}
//
//// 2. Разделение на train/test
//Console.WriteLine("\n📂 Разделение датасета...");
//DatasetUtilities.SplitDataset(datasetPath, trainPath, testPath, 0.8);
//
//// 3. Обучение модели
//Console.WriteLine("\n🤖 Обучение модели...");
//var trainer = new SentimentTrainer(modelPath);
//trainer.TrainModel(trainPath);
//
//// 4. Оценка качества
//Console.WriteLine("\n📈 Оценка качества на test set...");
//var builder = trainer.GetModelBuilder();
//var metrics = builder.Evaluate(testPath);
//
//builder.SaveModel(modelPath);
//
//Console.WriteLine($"\n=== Метрики регрессии ===");
//Console.WriteLine($"MAE (Mean Absolute Error): {metrics.MeanAbsoluteError:F4}");
//Console.WriteLine($"RMSE (Root Mean Squared Error): {metrics.RootMeanSquaredError:F4}");
//Console.WriteLine($"R² (R-squared): {metrics.RSquared:F4}");


//5. Пример предсказания
var service = new SentimentAnalysisService(new SentimentModelBuilder());
service.InitializeModel(modelPath);


var lines = File.ReadAllLines("Data/posts_export.csv");

StringBuilder sb = new StringBuilder();

StringBuilder sb1 = new StringBuilder();

foreach (var line in lines)
{
  var parts = line.Split(';');
  if (parts.Length < 2) continue;
  var id = parts[0];
  var text = parts[1];
  var pred = await service.AnalyzeSentimentAsync(text);
  sb.AppendLine($"{id};{pred.Score:F4};{text}");
  sb1.AppendLine($"{id};{pred.Score:F4}");
  Console.WriteLine($"Post ID: {id}");
  Console.WriteLine($"Text: {text}");
  Console.WriteLine($"Emotion: {pred.Emotion}, Score: {pred.Score}");
  Console.WriteLine(new string('-', 40));
}

File.WriteAllText("Data/posts_with_predictions.csv", sb.ToString());
File.WriteAllText("Data/posts_with_scores.csv", sb1.ToString());

//var prediction = await service.AnalyzeSentimentAsync(
//    "Компания показала рекордный рост прибыли"
//);
//
//Console.WriteLine(prediction.FormatPrediction());
//
//var texts = new[]
//{
//    "Убытки компании растут",
//    "Акции торгуются стабильно",
//    "Новый продукт имеет успех"
//};
//
//var predictions = await service.AnalyzeSentimentBatchAsync(texts);
//
//foreach (var pred in predictions)
//{
//  Console.WriteLine($"📝 {pred.Text}");
//  Console.WriteLine($"🎯 {pred.Emotion}");
//  Console.WriteLine($"📈 Score: {pred.Score}");
//}
