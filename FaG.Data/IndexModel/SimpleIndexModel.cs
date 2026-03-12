namespace FaG.Data.IndexModel
{
  public class SimpleIndexModel : BaseIndexModel
  {
    public SimpleIndexModel() : base("Simple") { }

    public override int ComputeScoreInt(int positive, int negative, int neutral)
    {
      // Default logic: positive +1, negative -1, neutral -1
      return positive * 1 + negative * 0 + neutral * -1;
    }

    // Normalize to [-1,1]
    public override double Normalize(int scoreInt, int effectivePosts)
    {
      if (effectivePosts <= 0)
        return 0.0;

      return (double)scoreInt / effectivePosts;
    }
  }
}
