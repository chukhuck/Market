using FaG.Data.DAL;

namespace FaG.Data.IndexModel
{
  public class SimpleIndexModel : BaseIndexModel
  {
    public SimpleIndexModel() : base("Simple") { }

    public SimpleIndexModel(List<FearGreedIndex> historicalData) : base("Simple") 
    {
      _historicalData = historicalData;
    }


    public const int EmaPeriod = 5;
    public const double InertiaFactor = 0.1; // 10 % нового значения, 90 % старого
    public const int NormalizationWindow = 30; // Окно нормализации в днях
    private readonly List<FearGreedIndex> _historicalData = [];

    public override FearGreedIndex CalculateForDay(List<PostEvaluation> dailyPosts)
    {
      var result = new FearGreedIndex
      {
        PositivePosts = dailyPosts.Count(p => p.Emotion == Emotion.Positive),
        NegativePosts = dailyPosts.Count(p => p.Emotion == Emotion.Negative),
        NeutralPosts = dailyPosts.Count(p => p.Emotion == Emotion.Neutral),
        TotalRelevantPosts = dailyPosts.Count(p => p.Emotion != Emotion.None)
      };

      // Рассчитываем коэффициент уверенности и долю нейтральных
      result.NeutralRatio = result.TotalRelevantPosts > 0
              ? (double)result.NeutralPosts / result.TotalRelevantPosts
              : 0.0;

      result.Confidence = 1.0 - result.NeutralRatio;

      // Базовый расчёт индекса
      result.RawIndex = CalculateRawIndex(result.PositivePosts, result.NegativePosts, result.NeutralPosts);

      // Сглаживание EMA
      result.SmoothedIndex = CalculateEma(result.RawIndex);

      // Кумулятивная сумма
      result.CumulativeIndex = CalculateCumulativeSum(result.SmoothedIndex);

      // Нормализованная кумулятивная (за последние 30 дней)
      result.NormalizedCumulative = CalculateNormalizedCumulative(result.CumulativeIndex);

      // Кумулятивная с инерцией (учитывает предыдущие значения)
      result.InertialCumulative = CalculateInertialCumulative(result.SmoothedIndex - 50);

      return result;
    }

    private double CalculateRawIndex(int positive, int negative, int neutral)
    {
      if (positive + negative == 0) return 50.0; // Нет мнений → нейтрально
      double rawRatio = (double)(positive - negative) / (positive + negative);
      double confidence = 1.0 - (double)neutral / (positive + negative + neutral);
      double index = 50.0 + 50.0 * rawRatio * confidence;
      return Math.Max(0.0, Math.Min(100.0, index));
    }

    private double CalculateEma(double currentValue)
    {
      double alpha = 2.0 / (EmaPeriod + 1);
      double _previousEma = _historicalData.Count > 0 ? _historicalData.Last().SmoothedIndex : 50.0;

      return alpha * currentValue + (1 - alpha) * _previousEma;
    }
    
    private double CalculateCumulativeSum(double smoothedIndex)
    {
      double adjustedValue = smoothedIndex - 50.0;

      double _cumulativeSum = _historicalData.Count > 0 ? _historicalData.Last().CumulativeIndex : 0.0;
      return _cumulativeSum + adjustedValue;
    }
    
    private double CalculateNormalizedCumulative(double currentCumulative)
    {
      if (_historicalData.Count < NormalizationWindow)
        return currentCumulative;
      // Берём среднее за последние 30 дней
      var recentData = _historicalData
          .Skip(Math.Max(0, _historicalData.Count - NormalizationWindow))
          .ToList();
      double averageCumulative = recentData.Average(d => d.CumulativeIndex);
      // Нормализуем относительно среднего
      return currentCumulative - averageCumulative;
    }
    
    
    private double CalculateInertialCumulative(double dailyContribution)
    {
      double _inertialCumulative = _historicalData.Count > 0 ? _historicalData.Last().InertialCumulative : 0.0;
      _inertialCumulative = (1 - InertiaFactor) * _inertialCumulative + InertiaFactor * dailyContribution;
      return _inertialCumulative * 100; // Умножаем для удобства визуализации
    }
  }
}
