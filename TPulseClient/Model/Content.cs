namespace TPulseClient.Model
{
  public class Content
  {
    public string? Text { get; set; }
    public List<Instrument>? Instruments { get; set; }
    public List<Image>? Images { get; set; }
  }
}
