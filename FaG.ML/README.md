# 🤖 Sentiment Analysis ML Model

Модель машинного обучения для анализа тональности финансовых новостей на русском языке.

## 📊 Описание

Модель классифицирует финансовые новости на 3 категории:
- **Negative (😞)** - отрицательная тональность (score < -0.25)
- **Neutral (😐)** - нейтральная тональность (-0.25 ≤ score ≤ 0.25)
- **Positive (😊)** - положительная тональность (score > 0.25)

## 📁 Структура

```
FaG.ML/
├── Models/
│   ├── TextSentiment.cs            # Input модель для загрузки данных
│   └── SentimentPrediction.cs      # Output модель с результатами
├── Services/
│   └── SentimentAnalysisService.cs # Сервис для использования модели
├── Training/
│   └── SentimentTrainer.cs         # Тренер для обучения модели
├── Utilities/
│   └── DatasetUtilities.cs         # Утилиты для работы с датасетом
├── SentimentModelBuilder.cs        # Основной класс для построения модели
└── Data/
    └── webofrussia.tsv             # Датасет финансовых новостей
```

## 🚀 Использование

### 1️⃣ Анализ датасета

```csharp
var stats = DatasetUtilities.AnalyzeDataset("Data/webofrussia.tsv");
Console.WriteLine(stats);
```

### 2️⃣ Валидация датасета

```csharp
if (DatasetUtilities.ValidateDataset("Data/webofrussia.tsv", out var errors))
{
    Console.WriteLine("✅ Датасет валиден");
}
else
{
    errors.ForEach(e => Console.WriteLine($"❌ {e}"));
}
```

### 3️⃣ Разделение на train/test

```csharp
DatasetUtilities.SplitDataset(
    "Data/webofrussia.tsv",
    "Data/train.tsv",
    "Data/test.tsv",
    trainRatio: 0.8
);
```

### 4️⃣ Обучение модели

```csharp
var trainer = new SentimentTrainer("./Models/SentimentModel.zip");
trainer.TrainModel("Data/webofrussia.tsv");
```

### 5️⃣ Использование модели для предсказания

```csharp
var service = new SentimentAnalysisService(new SentimentModelBuilder());
service.InitializeModel("./Models/SentimentModel.zip");

var prediction = await service.AnalyzeSentimentAsync(
    "Компания показала рекордный рост прибыли"
);

Console.WriteLine(prediction.FormatPrediction());
// Вывод: 😊 Положительная тональность (уверенность: 92%)
```

### 6️⃣ Batch предсказания

```csharp
var texts = new[]
{
    "Убытки компании растут",
    "Акции торгуются стабильно",
    "Новый продукт имеет успех"
};

var predictions = await service.AnalyzeSentimentBatchAsync(texts);

foreach (var pred in predictions)
{
    Console.WriteLine($"📝 {pred.Text}");
    Console.WriteLine($"🎯 {pred.Emotion}");
    Console.WriteLine($"📈 Уверенность: {pred.Confidence:P2}");
}
```

## 📊 Модель и Pipeline

### Обработка текста (Text Featurization)

1. **Нормализация** - приведение к нижнему регистру, удаление спецсимволов
2. **Токенизация** - разбиение на слова
3. **Удаление стоп-слов** - русский язык
4. **N-граммы** - учёт двугранных комбинаций слов
5. **Векторизация** - преобразование в числовые признаки (TF)

### Классификатор

- **Алгоритм**: SDCA Maximum Entropy (мультиклассовая классификация)
- **Оптимизация**: Stochastic Dual Coordinate Ascent
- **Преимущества**: 
  - Быстрое обучение
  - Хорошая точность
  - Хорошо работает с большими датасетами

## 🎯 API Reference

### SentimentModelBuilder

```csharp
public class SentimentModelBuilder
{
    // Обучение модели
    public ITransformer BuildAndTrain(string dataPath)

    // Предсказание для одного текста
    public SentimentPrediction Predict(string text)

    // Batch предсказание
    public IEnumerable<SentimentPrediction> PredictBatch(IEnumerable<string> texts)

    // Сохранение модели
    public void SaveModel(string modelPath)

    // Загрузка модели
    public void LoadModel(string modelPath)

    // Оценка качества
    public MulticlassClassificationMetrics Evaluate(string testDataPath)
}
```

### SentimentPrediction

```csharp
public class SentimentPrediction
{
    public string Text { get; set; }                    // Исходный текст
    public uint PredictedLabel { get; set; }           // Класс (0,1,2)
    public float[] Score { get; set; }                 // Вероятности для всех классов
    public Emotion Emotion { get; }                    // Категория эмоции
    public float Confidence { get; }                   // Уверенность [0-1]
    public SentimentScores SentimentScores { get; }    // Детальные оценки
}
```

### DatasetUtilities

```csharp
// Анализ датасета
public static DatasetStats AnalyzeDataset(string tsvPath)

// Валидация формата
public static bool ValidateDataset(string tsvPath, out List<string> errors)

// Разделение на train/test
public static void SplitDataset(
    string inputPath, 
    string trainPath, 
    string testPath, 
    double trainRatio = 0.8)
```

## 📈 Формат TSV датасета

```
title	score	link	summary	published	tickers
Заголовок новости	0.75	URL	Текст новости	2022-07-29	['TICKER']
```

Важные поля:
- **score**: Float от -1.0 до 1.0
- **summary**: Текст новости (анализируется модель)
- Остальные поля: метаинформация

## 🔧 Параметры обучения

```csharp
// Seed для воспроизводимости
_mlContext = new MLContext(seed: 42);

// N-граммы (двугранные комбинации слов)
ngramLength: 2

// Стоп-слова русского языка
StopWordsRemovingEstimator.Language.Russian
```

## 📊 Метрики оценки

Модель использует следующие метрики для оценки качества:

- **MacroAccuracy** - средняя точность по всем классам
- **MicroAccuracy** - точность по всем примерам
- **LogLoss** - кроссэнтропийная ошибка
- **LogLossReduction** - улучшение относительно базовой модели
- **Confusion Matrix** - матрица ошибок

## 🛠️ Интеграция в Blazor приложение

### Program.cs

```csharp
// Регистрация сервисов
builder.Services.AddScoped<SentimentModelBuilder>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>(sp =>
{
    var builder = sp.GetRequiredService<SentimentModelBuilder>();
    var service = new SentimentAnalysisService(builder);
    service.InitializeModel("./Models/SentimentModel.zip");
    return service;
});
```

### Component.razor

```razor
@inject ISentimentAnalysisService SentimentService

<div>
    <textarea @bind="inputText"></textarea>
    <button @onclick="AnalyzeSentiment">Анализировать</button>

    @if (prediction != null)
    {
        <p>@prediction.Emotion.GetEmotionEmoji() @prediction.Emotion.GetEmotionDescription()</p>
        <p>Уверенность: @prediction.Confidence:P2</p>
        <div style="@($"background-color: {prediction.Emotion.GetEmotionColor()}")">
            Negative: @prediction.SentimentScores.NegativeScore:P1<br>
            Neutral: @prediction.SentimentScores.NeutralScore:P1<br>
            Positive: @prediction.SentimentScores.PositiveScore:P1
        </div>
    }
</div>

@code {
    private string inputText = "";
    private SentimentPrediction? prediction;

    private async Task AnalyzeSentiment()
    {
        prediction = await SentimentService.AnalyzeSentimentAsync(inputText);
    }
}
```

## 📝 Примеры использования

### Анализ финансовых новостей

```csharp
var builder = new SentimentModelBuilder();
builder.LoadModel("./Models/SentimentModel.zip");

var newsTitles = new[]
{
    "Акции компании подорожали на 15% после хороших результатов",
    "Прибыль упала из-за конкуренции на рынке",
    "Компания сохраняет стабильную позицию на рынке"
};

foreach (var news in newsTitles)
{
    var result = builder.Predict(news);
    Console.WriteLine($"{news}");
    Console.WriteLine($"→ {result.FormatPrediction()}\n");
}
```

### Обучение и оценка модели

```csharp
// Разделяем датасет
DatasetUtilities.SplitDataset(
    "Data/webofrussia.tsv",
    "Data/train.tsv",
    "Data/test.tsv"
);

// Обучаем модель
var trainer = new SentimentTrainer();
trainer.TrainModel("Data/train.tsv");

// Оцениваем качество
var builder = trainer.GetModelBuilder();
var metrics = builder.Evaluate("Data/test.tsv");

Console.WriteLine($"Accuracy: {metrics.MacroAccuracy:P2}");
Console.WriteLine($"Log Loss: {metrics.LogLoss:F4}");
```

## ⚠️ Важные замечания

1. **Язык**: Модель обучена на русскоязычных новостях
2. **Домен**: Финансовые новости (может требовать переобучения для других доменов)
3. **Стоп-слова**: Используются русские стоп-слова
4. **Формат данных**: Обязателен TSV с разделителем Tab
5. **Размер модели**: ~2-3 МБ (сохраняется в zip формате)

## 🚀 Рекомендации

- Обновляйте датасет новыми примерами
- Периодически переобучайте модель
- Используйте test dataset для оценки
- Мониторьте уверенность предсказаний
- Следите за distribution классов в данных

## 📋 Расширенные примеры

### Пример 1: Полный цикл обучения

```csharp
public class TrainingExample
{
    public static void RunFullTraining()
    {
        var datasetPath = "Data/webofrussia.tsv";
        var trainPath = "Data/train.tsv";
        var testPath = "Data/test.tsv";
        var modelPath = "./Models/SentimentModel.zip";

        // 1. Анализ исходного датасета
        Console.WriteLine("📊 Анализ датасета:");
        var stats = DatasetUtilities.AnalyzeDataset(datasetPath);
        Console.WriteLine(stats);

        // 2. Разделение на train/test
        Console.WriteLine("\n📂 Разделение датасета...");
        DatasetUtilities.SplitDataset(datasetPath, trainPath, testPath, 0.8);

        // 3. Обучение модели
        Console.WriteLine("\n🤖 Обучение модели...");
        var trainer = new SentimentTrainer(modelPath);
        trainer.TrainModel(trainPath);

        // 4. Оценка качества
        Console.WriteLine("\n📈 Оценка качества на test set...");
        var builder = trainer.GetModelBuilder();
        var metrics = builder.Evaluate(testPath);

        Console.WriteLine($"Macro Accuracy: {metrics.MacroAccuracy:P2}");
        Console.WriteLine($"Micro Accuracy: {metrics.MicroAccuracy:P2}");
        Console.WriteLine($"Log Loss: {metrics.LogLoss:F4}");
    }
}
```

### Пример 2: Реальное время анализ потока новостей

```csharp
public class NewsStreamAnalyzer
{
    private readonly ISentimentAnalysisService _service;

    public NewsStreamAnalyzer(ISentimentAnalysisService service)
    {
        _service = service;
    }

    public async Task AnalyzeNewsFeedAsync(IAsyncEnumerable<string> newsFeed)
    {
        var buffer = new List<string>();

        await foreach (var news in newsFeed)
        {
            buffer.Add(news);

            // Batch обработка каждые 100 новостей
            if (buffer.Count >= 100)
            {
                var predictions = await _service.AnalyzeSentimentBatchAsync(buffer);

                var stats = new
                {
                    Positive = predictions.Count(p => p.Emotion == Emotion.Positive),
                    Neutral = predictions.Count(p => p.Emotion == Emotion.Neutral),
                    Negative = predictions.Count(p => p.Emotion == Emotion.Negative)
                };

                Console.WriteLine($"Processed 100 news - Positive: {stats.Positive}, Neutral: {stats.Neutral}, Negative: {stats.Negative}");
                buffer.Clear();
            }
        }
    }
}
```

## 🔗 Связанные проекты

- **FaG.WebClient** - Blazor фронтенд приложение
- **FaG.EmotionApi** - REST API для анализа тональности
- **FaG.Scheduler** - Планировщик для переобучения модели

