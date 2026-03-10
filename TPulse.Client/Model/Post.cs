namespace TPulse.Client.Model
{
  public class Post
  {
    public Guid Id { get; set; }
    public int CommentsCount { get; set; }
    public DateTime Inserted { get; set; }
    public Owner? Owner { get; set; }
    public Reactions? Reactions { get; set; }
    public Content? Content { get; set; }
  }
}
