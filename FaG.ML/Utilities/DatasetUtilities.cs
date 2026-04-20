using FaG.ML.Models;
using System.Globalization;

namespace FaG.ML.Utilities
{
  /// <summary>
  /// Utilities for working with sentiment datasets
  /// </summary>
  public static class DatasetUtilities
  {
    /// <summary>
    /// Loads dataset statistics
    /// </summary>
    public static DatasetStats AnalyzeDataset(string tsvPath)
    {
      var stats = new DatasetStats();
      var sentimentCounts = new Dictionary<uint, int> { { 0, 0 }, { 1, 0 }, { 2, 0 } };

      try
      {
        using var reader = new StreamReader(tsvPath);
        // Skip header
        reader.ReadLine();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
          var parts = line.Split('\t');
          if (parts.Length < 2)
            continue;

          stats.TotalRecords++;

          if (float.TryParse(parts[1], NumberStyles.Float|NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var score))
          {
            stats.ScoreSum += score;
            stats.ScoreMin = Math.Min(stats.ScoreMin, score);
            stats.ScoreMax = Math.Max(stats.ScoreMax, score);

            uint label = score switch
            {
              < -0.33f => 0, // Negative
              <= 0.33f => 1, // Neutral
              _ => 2          // Positive
            };

            if (sentimentCounts.ContainsKey(label))
              sentimentCounts[label]++;
          }
        }

        stats.AverageScore = stats.TotalRecords > 0 ? stats.ScoreSum / stats.TotalRecords : 0;
        stats.NegativeCount = sentimentCounts[0];
        stats.NeutralCount = sentimentCounts[1];
        stats.PositiveCount = sentimentCounts[2];
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Failed to analyze dataset: {ex.Message}", ex);
      }

      return stats;
    }

    /// <summary>
    /// Splits dataset into train and test sets
    /// </summary>
    public static void SplitDataset(string inputPath, string trainPath, string testPath, double trainRatio = 0.8)
    {
      var lines = File.ReadAllLines(inputPath).ToList();
      if (lines.Count == 0)
        throw new InvalidOperationException("Input file is empty");

      var header = lines[0];
      var dataLines = lines.Skip(1).ToList();

      var random = new Random(42); // Reproducible split
      var shuffled = dataLines.OrderBy(x => random.Next()).ToList();

      var trainCount = (int)(shuffled.Count * trainRatio);
      var trainLines = shuffled.Take(trainCount).ToList();
      var testLines = shuffled.Skip(trainCount).ToList();

      // Write train set
      File.WriteAllLines(trainPath, new[] { header }.Concat(trainLines));

      // Write test set
      File.WriteAllLines(testPath, new[] { header }.Concat(testLines));

      Console.WriteLine($"✅ Dataset split completed:");
      Console.WriteLine($"   Train: {trainPath} ({trainLines.Count} records)");
      Console.WriteLine($"   Test: {testPath} ({testLines.Count} records)");
    }

    /// <summary>
    /// Validates dataset format
    /// </summary>
    public static bool ValidateDataset(string tsvPath, out List<string> errors)
    {
      errors = new List<string>();

      if (!File.Exists(tsvPath))
      {
        errors.Add($"File not found: {tsvPath}");
        return false;
      }

      var lines = File.ReadAllLines(tsvPath);
      if (lines.Length == 0)
      {
        errors.Add("File is empty");
        return false;
      }

      var headerColumns = lines[0].Split('\t');
      if (headerColumns.Length < 2)
      {
        errors.Add($"Expected 2 columns, found {headerColumns.Length}");
        return false;
      }

      var invalidRows = 0;
      for (int i = 1; i < Math.Min(lines.Length, 100); i++) // Check first 100 rows
      {
        var parts = lines[i].Split('\t');
        if (parts.Length < 2)
        {
          invalidRows++;
          errors.Add($"Row {i + 1}: Invalid tab '{lines[i]}'");
          continue;
        }

        if (!float.TryParse(parts[1], NumberStyles.Float | NumberStyles.Number, CultureInfo.InvariantCulture, out var score) ||
            score < -1f || score > 1f)
        {
          invalidRows++;
          errors.Add($"Row {i + 1}: Invalid score value '{parts[1]}'");
        }
      }

      if (invalidRows > 0)
        errors.Add($"Found {invalidRows} rows with invalid score values");

      return errors.Count == 0;
    }
  }

  /// <summary>
  /// Statistics about the dataset
  /// </summary>
  public class DatasetStats
  {
    public int TotalRecords { get; set; }
    public float ScoreMin { get; set; } = 1f;
    public float ScoreMax { get; set; } = -1f;
    public float ScoreSum { get; set; }
    public float AverageScore { get; set; }
    public int NegativeCount { get; set; }
    public int NeutralCount { get; set; }
    public int PositiveCount { get; set; }

    public override string ToString()
    {
      var separator = new string('=', 40);
      return $@"
📊 Dataset Statistics
{separator}
Total Records:        {TotalRecords:N0}
Score Range:         [{ScoreMin:F3}, {ScoreMax:F3}]
Average Score:       {AverageScore:F3}

Distribution:
  Negative:          {NegativeCount:N0} ({100.0 * NegativeCount / TotalRecords:F1}%)
  Neutral:           {NeutralCount:N0} ({100.0 * NeutralCount / TotalRecords:F1}%)
  Positive:          {PositiveCount:N0} ({100.0 * PositiveCount / TotalRecords:F1}%)
";
    }
  }
}
