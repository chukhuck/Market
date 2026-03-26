namespace FaG.Data.IndexModel
{
  public abstract class BaseIndexModel(string name)
  {
    public string Name { get; } = name;

    public abstract double CalculateIndex(int positive, int negative, int neutral);
  }
}
