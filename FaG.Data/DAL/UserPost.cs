using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaG.Data.DAL
{
  [Table("UserPosts")]
  public class UserPost
  {
    [Key]
    public int Id { get; set; }
    public string InnerId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid AuthorId { get; set; } = Guid.Empty;
    public string AuthorNickname { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Lenght { get; set; }
    public int CommentsCount { get; set; }
    public int TotalReactions { get; set; }
    public string ReactionsJson { get; set; } = string.Empty;
    public string Tickers { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;


    public HashSet<PostEvaluation> Evaluations { get; set; } = new HashSet<PostEvaluation>();
  }
}
