using FaG.ML.RussianFinancialNewsParser;
using Parquet;
using Parquet.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


partial class Program
{
  static async Task Main(string[] args)
  {
    try
    {
      var newsItems = await LoadNewsFromParquetAsync("RussianFinancialNews/news_collection.parquet");
      var llmDescriptions = LoadDescriptionsFromJson("RussianFinancialNews/news_descriptions/news_description_LLama3_8b.json");
      var gptDescriptions = LoadDescriptionsFromJson("RussianFinancialNews/news_descriptions/news_descriptions_GPT4o.json");
      var yandexDescriptions = LoadDescriptionsFromCSV("RussianFinancialNews/news_descriptions/news_descriptions_yandex.csv");

      var combinedItems = CombineData(newsItems, llmDescriptions, gptDescriptions, yandexDescriptions);
      var zeroItems = combinedItems.Where(i=>i.SentimentScoreYandex == 0).ToList();
      combinedItems = combinedItems.Where(i => i.SentimentScoreYandex != 0).ToList();

      zeroItems = zeroItems.OrderByDescending(i => i.Body.Length).Take(150).ToList();
      combinedItems.AddRange(zeroItems);

      var zeroItems1 = combinedItems.Where(i => i.SentimentScoreYandex >= 0.2 && i.SentimentScoreYandex < 0.4).ToList();
      combinedItems = combinedItems.Where(i => i.SentimentScoreYandex < 0.2 || i.SentimentScoreYandex >= 0.4).ToList();

      zeroItems1 = zeroItems1.OrderByDescending(i => i.Body.Length).Take(500).ToList();
      combinedItems.AddRange(zeroItems1);

      var zeroItems2 = combinedItems.Where(i => i.SentimentScoreYandex >= 0.4 && i.SentimentScoreYandex < 0.6).ToList();
      combinedItems = combinedItems.Where(i => i.SentimentScoreYandex < 0.4 || i.SentimentScoreYandex >= 0.6).ToList();

      zeroItems2 = zeroItems2.OrderByDescending(i => i.Body.Length).Take(500).ToList();
      combinedItems.AddRange(zeroItems2);

      var zeroItems3 = combinedItems.Where(i => i.SentimentScoreYandex >= 0.6 && i.SentimentScoreYandex < 0.8).ToList();
      combinedItems = combinedItems.Where(i => i.SentimentScoreYandex < 0.6 || i.SentimentScoreYandex >= 0.8).ToList();

      zeroItems3 = zeroItems.OrderByDescending(i => i.Body.Length).Take(500).ToList();
      combinedItems.AddRange(zeroItems3);


      // Разбиваем на корзины с шагом 0.2
      var buckets = new Dictionary<double, List<CombinedNewsItem>>();
      double bucketSize = 0.2;


      foreach (var item in combinedItems)
      {
        // Определяем границу корзины (округляем вниз до ближайшего кратного 0.2)
        double bucketKey = Math.Floor(item.SentimentScoreYandex / bucketSize) * bucketSize;

        // Корректируем крайние значения: если score == 1.0, относим к корзине [0.8, 1.0]
        if (bucketKey >= 1.0) bucketKey = 0.8;

        if (!buckets.ContainsKey(bucketKey))
          buckets[bucketKey] = new List<CombinedNewsItem>();

        buckets[bucketKey].Add(item);
      }

      Console.WriteLine("Распределение элементов по корзинам:");
      Console.WriteLine("Диапазон\t\tКоличество элементов");
      Console.WriteLine("-------------------------------------");

      var sortedBuckets = buckets.OrderBy(b => b.Key);
      foreach (var bucket in sortedBuckets)
      {
        double lowerBound = bucket.Key;
        double upperBound = bucket.Key + bucketSize;
        Console.WriteLine($"[{lowerBound:F1}, {upperBound:F1})\t\t{bucket.Value.Count}");
      }

       bucketSize = 0.66;
       buckets = new Dictionary<double, List<CombinedNewsItem>>();

      foreach (var item in combinedItems)
      {
        // Определяем границу корзины (округляем вниз до ближайшего кратного 0.2)
        double bucketKey = item.SentimentLabelYandex;

        // Корректируем крайние значения: если score == 1.0, относим к корзине [0.8, 1.0]
        //if (bucketKey >= 0.9) bucketKey = 2;

        if (!buckets.ContainsKey(bucketKey))
          buckets[bucketKey] = new List<CombinedNewsItem>();

        buckets[bucketKey].Add(item);
      }

      Console.WriteLine("Распределение элементов по корзинам:");
      Console.WriteLine("Диапазон\t\tКоличество элементов");
      Console.WriteLine("-------------------------------------");

      sortedBuckets = buckets.OrderBy(b => b.Key);
      foreach (var bucket in sortedBuckets)
      {
        double lowerBound = bucket.Key* bucketSize - 1;
        double upperBound = bucket.Key * bucketSize + bucketSize ;
        Console.WriteLine($"[{lowerBound:F1}, {upperBound:F1})\t\t{bucket.Value.Count}");
      }


      // Берём не более 600 элементов из каждой корзины
      int maxElementsPerBucket = 741;
      var balancedItems = new List<CombinedNewsItem>();

      foreach (var bucket in buckets.Values)
      {
        // Перемешиваем элементы корзины для случайности выборки
        var shuffledBucket = bucket.OrderBy(bi => Guid.NewGuid()).ToList();
        // Берём первые maxElementsPerBucket элементов (или все, если их меньше)
        balancedItems.AddRange(shuffledBucket.Take(maxElementsPerBucket));//
      }

      // Сохраняем сбалансированный набор
      SaveToTsv(balancedItems, "RussianFinancialNews_ya_balance.tsv");
      SaveToTsv2(balancedItems, "RussianFinancialNews_ya_balance_body.tsv");

      // Выводим статистику после балансировки
      Console.WriteLine($"\nСтатистика после балансировки (максимум {maxElementsPerBucket} элементов на корзину):");
      Console.WriteLine("Диапазон\t\tВзято элементов");
      Console.WriteLine("-------------------------------------");

      foreach (var bucket in sortedBuckets)
      {
        double lowerBound = bucket.Key;
        double upperBound = bucket.Key + bucketSize;
        int taken = Math.Min(bucket.Value.Count, maxElementsPerBucket);
        Console.WriteLine($"[{lowerBound:F1}, {upperBound:F1})\t\t{taken}");
      }

      Console.WriteLine($"\nВсего элементов в итоговом наборе: {balancedItems.Count}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка: {ex.Message}");
    }
  }

  static async Task<Dictionary<long, NewsItem>> LoadNewsFromParquetAsync(string filePath)
  {
    var newsItems = new Dictionary<long, NewsItem>();

    using (var fileStream = File.OpenRead(filePath))
    {
      using (var parquetReader = await ParquetReader.CreateAsync(fileStream))
      {
        int rowGroupCount = parquetReader.RowGroupCount;

        for (int rg = 0; rg < rowGroupCount; rg++)
        {
          using (var rowGroupReader = parquetReader.OpenRowGroupReader(rg))
          {
            var dataFields = parquetReader.Schema.GetDataFields();
            var columns = new List<DataColumn>();


            foreach (var field in dataFields)
            {
              var column = await rowGroupReader.ReadColumnAsync(field);
              columns.Add(column);
            }

            // Получаем данные по колонкам
            var idColumn = columns.First(c => c.Field.Name == "__index_level_0__").Data.Cast<long>().ToArray();
            var titleColumn = columns.First(c => c.Field.Name == "title").Data.Cast<string>().ToArray();
            var bodyColumn = columns.First(c => c.Field.Name == "body").Data.Cast<string>().ToArray();
            var dateColumn = columns.First(c => c.Field.Name == "date").Data.Cast<string>().ToArray();
            var timeColumn = columns.First(c => c.Field.Name == "time").Data.Cast<string>().ToArray();
            var tagsColumn = columns.First(c => c.Field.Name == "tags").Data.Cast<string>().ToArray();
            var sourceColumn = columns.First(c => c.Field.Name == "source").Data.Cast<string>().ToArray();

            int rowCount = idColumn.Length;

            for (int i = 0; i < rowCount; i++)
            {
              newsItems[idColumn[i]] = new NewsItem
              {
                Id = idColumn[i],
                Title = CleanText(titleColumn[i]) == "no title" ? " " : CleanText(titleColumn[i]),
                Body = CleanText(bodyColumn[i]),
                Date = DateTime.Parse(dateColumn[i]),
                Time = timeColumn[i],
                Tags = (tagsColumn[i]?.Split(',').Select(t => t.Trim()).ToList()) ?? new List<string>(),
                Source = sourceColumn[i]
              };
            }
          }
        }
      }

      return newsItems;
    }
  }

  

static Dictionary<long, DescriptionItem> LoadDescriptionsFromCSV(string filePath)
{
    var descriptions = new Dictionary<long, DescriptionItem>();
    using var reader = new StreamReader(filePath);

    string? line;
    int counter = 0;
    while ((line = reader.ReadLine()) != null)
    {
      var parts = line.Split(' ');
      if (parts.Length != 2)
        continue;

      if (float.TryParse(parts[1], NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var score))
      {
        var trueScore = score;

        if (score > 0 && score <= 0.2 && counter++ % 2 == 0)//
        {
          trueScore = -score;
        }


        descriptions[long.Parse(parts[0])] = new DescriptionItem
        {
          SentimentScore = trueScore
        };
      }
    }

    return descriptions;
  }

static Dictionary<long, DescriptionItem> LoadDescriptionsFromJson(string filePath)
  {
    var descriptions = new Dictionary<long, DescriptionItem>();
    var jsonContent = File.ReadAllText(filePath);
    var root = JsonDocument.Parse(jsonContent).RootElement;

    foreach (var property in root.EnumerateObject())
    {
      if (int.TryParse(property.Name, out int id))
      {
        var descElement = property.Value;
        // Для LLama: описание вложено в "description"
        if (descElement.TryGetProperty("description", out var descProp))
          descElement = descProp;

        descriptions[id] = new DescriptionItem
        {
          ArticleType = descElement.GetProperty("article_type").GetString(),
          Country = GetStringList(descElement, "country"),
          Sectors = GetStringList(descElement, "sectors"),
          Tickers = GetStringList(descElement, "tickers"),
          SentimentScore = descElement.GetProperty("sentiment_score").GetDouble()
        };
      }
    }
    return descriptions;
  }

  static List<string> GetStringList(JsonElement element, string propertyName)
  {
    if (!element.TryGetProperty(propertyName, out var prop) || !prop.ValueKind.Equals(JsonValueKind.Array))
      return new List<string>();

    return [.. prop.EnumerateArray()
    .Select(x => x.GetString()?.Trim().ToLowerInvariant() ?? string.Empty)
    .Where(x => !string.IsNullOrEmpty(x))
    .Distinct(StringComparer.OrdinalIgnoreCase)];
  }

  static List<CombinedNewsItem> CombineData(
Dictionary<long, NewsItem> newsItems,
Dictionary<long, DescriptionItem> llmDescriptions,
Dictionary<long, DescriptionItem> gptDescriptions,
Dictionary<long, DescriptionItem> yandexDescriptions)
  {
    var combinedItems = new List<CombinedNewsItem>();

    foreach (var newsItem in newsItems)
    {
      if (!yandexDescriptions.ContainsKey(newsItem.Key))
        continue;

      var llmDesc = llmDescriptions[newsItem.Key];
      var gptDesc = gptDescriptions[newsItem.Key];
      var yandexDesc = yandexDescriptions[newsItem.Key];

      // Объединяем поля с удалением дубликатов без учёта регистра
      var combinedArticleType = llmDesc.ArticleType ?? gptDesc.ArticleType ?? "unk";
      var combinedCountry = (llmDesc.Country ?? [])
      .Concat(gptDesc.Country ?? [])
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
      var combinedSectors = (llmDesc.Sectors ?? [])
      .Concat(gptDesc.Sectors ?? [])
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
      var combinedTickers = (llmDesc.Tickers ?? [])
      .Concat(gptDesc.Tickers ?? [])
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

      // Среднее sentiment score
      var averageSentiment = (llmDesc.SentimentScore + gptDesc.SentimentScore + yandexDesc.SentimentScore) / 3;

      // Label по правилам
      int sentimentLabel = averageSentiment < -0.33 ? 0 :
      (averageSentiment > 0.33 ? 2 : 1);

      combinedItems.Add(new CombinedNewsItem
      {
        Id = newsItem.Value.Id,
        Date = newsItem.Value.Date,
        Time = newsItem.Value.Time,
        Tags = newsItem.Value.Tags,
        Source = newsItem.Value.Source,
        ArticleType = combinedArticleType,
        Country = combinedCountry,
        Sectors = combinedSectors,
        Tickers = combinedTickers,
        SentimentScore = averageSentiment,
        SentimentScoreGPT = gptDesc.SentimentScore,
        SentimentScoreLLama = llmDesc.SentimentScore,
        SentimentScoreYandex = yandexDesc.SentimentScore,
        SentimentLabelYandex = yandexDesc.SentimentScore < -0.33 ? 0 : (yandexDesc.SentimentScore > 0.33 ? 2 : 1),
        SentimentLabel = sentimentLabel,
        Title = ClearText(newsItem.Value.Title),
        Body = ClearText( newsItem.Value.Body)
      });
    }

    return combinedItems;
  }

  private static string ClearText(string? body)
  {
    return string.IsNullOrEmpty(body) ? string.Empty : 
      body
      .Replace("#", "")
      .Replace("\'", "")
      .Replace("\"", "")
      .Replace("\t", " ")
      .Replace("\r", " ")
      .Replace("\n", " ").Trim();
  }

  static void SaveToTsv(List<CombinedNewsItem> items, string filePath)
  {
    using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
    {
      // Заголовки
      writer.WriteLine("id\tdate\ttime\ttags\tsource\tarticle_type\tcountry\tsectors\tticker\tsentiment_score\tsentiment_score_gpt\tsentiment_score_llama\tsentiment_score_yandex\ttsentiment_label_yandex\tsentiment_label\ttitle\tbody");
      foreach (var item in items)
      {
        writer.WriteLine($"{item.Id}\t{item.Date:yyyy-MM-dd}\t{item.Time}\t" +
        $"{string.Join(';', item.Tags ?? ["unk"])}\t{item.Source}\t" +
        $"{item.ArticleType}\t" +
        $"{string.Join(';', item.Country ?? ["unk"])}\t" +
        $"{string.Join(';', item.Sectors ?? ["unk"])}\t" +
        $"{string.Join(';', item.Tickers ?? ["unk"])}\t" +
        $"{item.SentimentScore.ToString("F4", CultureInfo.InvariantCulture)}\t" +
        $"{item.SentimentScoreGPT.ToString("F4", CultureInfo.InvariantCulture)}\t" +
        $"{item.SentimentScoreLLama.ToString("F4", CultureInfo.InvariantCulture)}\t" +
        $"{item.SentimentScoreYandex.ToString("F4", CultureInfo.InvariantCulture)}\t" +
        $"{item.SentimentLabelYandex}\t" +
        $"{item.SentimentLabel}\t" +
        $"{item.Title}\t{item.Body}");
      }
    }
  }

  static void SaveToTsv2(List<CombinedNewsItem> items, string filePath)
  {
    using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
    {
      // Заголовки
      writer.WriteLine("id\tsentiment_score\tbody");
      foreach (var item in items)
      {
        writer.WriteLine($"{item.Id}\t{item.SentimentScoreYandex.ToString("F2", CultureInfo.InvariantCulture)}\t{item.Title} {item.Body}");
      }
    }
  }

  static string CleanText(string text)
  {
    if (string.IsNullOrEmpty(text))
      return text;

    // Удаляем переводы каретки и табуляции
    text = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("«", "").Replace("»", "");


    // 1. Удаляем хештеги и упоминания (@ и #)
    text = Regex.Replace(text, @"\s*[@#]\w+", string.Empty);

    // 3. Удаляем эмодзи по Unicode‑диапазонам
    text = RemoveEmojis(text);

    // 4. Удаляем лишние спецсимволы, но сохраняем кириллицу, латиницу, цифры, пробелы и знаки препинания
    text = Regex.Replace(text, @"[^\p{L}\p{N}\s\.\!\?\,\-\—\:\;\(\)\[\]]", " ");

    // 3. Удаляем тикеры компаний в скобках
    text = Regex.Replace(text, @"\([^)]*\)", string.Empty);

    // 4. Удаляем множественные пробелы и обрезаем строку
    text = Regex.Replace(text, @"\s+", " ").Trim();

    // Убираем лишние пробелы
    return Regex.Replace(text, @"\s+", " ").Trim();
  }


  public static string RemoveEmojis(string text)
  {
    if (string.IsNullOrEmpty(text))
      return text;

    // Диапазоны эмодзи в Unicode
    var emojiPattern = 
            "\U0001F600-\U0001F64F|\U0001F300-\U0001F5FF" + // символы и пиктограммы
            "|\U0001F680-\U0001F6FF" + // транспорт и символы
            "|\U0001F1E0-\U0001F1FF" + // флаги стран
            "|\U00002702-\U000027B0" + // дингбаты
            "|\U000024C2-\U0001F251" + // разные символы
            "|\U0001F910-\U0001F96B" + // дополнительные эмодзи
            "|\U0001F980-\U0001F9E0]"; // животные, предметы и т. д.
  
    return Regex.Replace(text, emojiPattern, string.Empty);
  }
}
