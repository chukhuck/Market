using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaG.Data.DAL
{
  [Table("FearGreedIndices")]
  public class FearGreedIndex
  {
    [Key]
    public int Id { get; set; }

    // Date for which the index is computed (UTC date, date portion used)
    public DateTime DateUtc { get; set; }

    // Integer score (sum of +1/-1 counts)
    public int ScoreInt { get; set; }

    // Normalized score in [-1,1]
    public double ScoreNormalized { get; set; }

    // Counts
    public int TotalPosts { get; set; }
    public int PositivePosts { get; set; }
    public int NegativePosts { get; set; }
    public int NeutralPosts { get; set; }
    public int UnratedPosts { get; set; }

    // Model name used to compute the index
    public string ModelName { get; set; } = string.Empty;
  }
}
