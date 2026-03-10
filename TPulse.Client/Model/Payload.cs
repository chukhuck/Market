using System.Text.Json.Serialization;

namespace TPulse.Client.Model
{
  public class Payload
  {
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? NextCursor { get; set; }
    public bool HasNext { get; set; }
    public List<Post>? Items { get; set; }
  }
}
