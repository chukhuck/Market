using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.RussianFinancialNewsParser
{
  public class NewsItem
  {
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string? Time { get; set; }
    public List<string>? Tags { get; set; }
    public string? Source { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
  }

  public class DescriptionItem
  {
    public string? ArticleType { get; set; }
    public List<string>? Country { get; set; }
    public List<string>? Sectors { get; set; }
    public List<string>? Tickers { get; set; }
    public double SentimentScore { get; set; }
  }

  public class CombinedNewsItem
  {
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string? Time { get; set; }
    public List<string>? Tags { get; set; }
    public string? Source { get; set; }
    public string? ArticleType { get; set; }
    public List<string>? Country { get; set; }
    public List<string>? Sectors { get; set; }
    public List<string>? Tickers { get; set; }
    public double SentimentScore { get; set; }
    public double SentimentScoreGPT { get; set; }
    public double SentimentScoreLLama { get; set; }
    public double SentimentScoreYandex { get; set; }
    public int SentimentLabel { get; set; }
    public int SentimentLabelYandex { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
  }
}
