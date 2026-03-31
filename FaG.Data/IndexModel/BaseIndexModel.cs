using FaG.Data.DAL;

namespace FaG.Data.IndexModel
{
  public abstract class BaseIndexModel(string name)
  {
    public string Name { get; } = name;

    public abstract FearGreedIndex CalculateForDay(List<PostEvaluation> dailyPosts);
  }
}
