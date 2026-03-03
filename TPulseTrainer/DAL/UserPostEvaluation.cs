using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TPulseTrainer.DAL
{


  [Table("UserPostEvaluations")]
  public class UserPostEvaluation
  {
    [Key]
    public int Id { get; set; }
    public Guid PostId { get; set; } = Guid.Empty;
    public DateTime EvaluationDate { get; set; }
    public Emotion Emotion { get; set; }
    public Guid AuthorId { get; set; } = Guid.Empty;
    public string AuthorNickname { get; set; } = string.Empty;
    public string PostText { get; set; } = string.Empty;
    public int CommentsCount { get; set; }
    public int TotalReactions { get; set; }
    public string ReactionsJson { get; set; } = string.Empty;
    public string Tickers { get; set; } = string.Empty;
  }

  public enum Emotion
  {
    Positive,
    Negative,
    Neutral,
    Skipped
  }
}
