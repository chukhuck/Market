using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaG.Data.DAL
{
  [Table("IMOEX")]
  public class IMOEXIndexTradeDay
  {
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; }

    public double Open { get; set; }
    public double Close { get; set; }
    public double Low { get; set; }
    public double High { get; set; }
    public double Volume { get; set; }
  }
}
