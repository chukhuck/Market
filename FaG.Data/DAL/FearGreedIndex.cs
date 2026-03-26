using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaG.Data.DAL
{
  [Table("FearGreedIndices")]
  public class FearGreedIndex
  {
    [Key]
    public int Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public DateTime Date { get; set; }


    // Исходные данные
    public int PositivePosts { get; set; }
    public int NegativePosts { get; set; }
    public int NeutralPosts { get; set; }
    public int UnratedPosts { get; set; }
    public int TotalRelevantPosts { get; set; } // positive + negative + neutral

    // Расчётные показатели
    public double RawIndex { get; set; } // Индекс 0–100 без сглаживания
    public double SmoothedIndex { get; set; } // EMA от RawIndex
    public double CumulativeIndex { get; set; } // Кумулятивная сумма
    public double NormalizedCumulative { get; set; } // Нормализованная кумулятивная сумма
    public double InertialCumulative { get; set; } // Кумулятивная с инерцией

    // Вспомогательные метрики для анализа
    public double NeutralRatio { get; set; } // Доля нейтральных постов
    public double Confidence { get; set; } // Коэффициент уверенности
    public bool IsExtremeFear { get; set; } // RawIndex <= 25
    public bool IsExtremeGreed { get; set; } // RawIndex >= 75
  }
}
