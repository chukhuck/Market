using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaG.Data.DAL
{
  [Table("PostEvaluations")]
  public class PostEvaluation
  {
    [Key]
    public int Id { get; set; }
    public int PostId { get; set; }
    public DateTime Date { get; set; }
    public long Longiness { get; set; } = 0;
    public Emotion Emotion { get; set; }
    public string Evaluator { get; set; } = string.Empty;

    public UserPost Post { get; set; } = null!;
    public float Score { get; set; }
  }
}
