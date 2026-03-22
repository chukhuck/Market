using FaG.Data.DAL;

namespace FaG.Data.IndexModel
{
  public abstract class BaseIndexModel(string name)
  {
    public string Name { get; } = name;

    public abstract int ComputeScoreInt(int positive, int negative, int neutral);

    public abstract double Normalize(int scoreInt, int effectivePosts);
  }
}
