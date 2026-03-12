using FaG.Data.DAL;

namespace FaG.Data.IndexModel
{
  public abstract class BaseIndexModel
  {
    public string Name { get; }

    protected BaseIndexModel(string name)
    {
      Name = name;
    }

    public abstract int ComputeScoreInt(int positive, int negative, int neutral);

    public abstract double Normalize(int scoreInt, int effectivePosts);
  }
}
