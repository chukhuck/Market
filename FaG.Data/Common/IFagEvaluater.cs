using FaG.Data.DAL;

namespace FaG.Data.Common
{
  public interface IFagEvaluater
  {
    public Task<PostEvaluation> EvaluateAsync(UserPost post, CancellationToken token = default);
  }
}
