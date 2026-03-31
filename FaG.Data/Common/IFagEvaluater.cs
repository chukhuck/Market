using FaG.Data.DAL;

namespace FaG.Data.Common
{
  public interface IFagEvaluater
  {
    public string Name { get; set; }
    public Task<PostEvaluation?> EvaluateAsync(UserPost post, CancellationToken token = default);
  }
}
