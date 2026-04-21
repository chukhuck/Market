using FaG.Data.Common;
using FaG.Data.DAL;
using System.Text;
using System.Text.Json;

namespace FaG.Scheduler.Services
{
  public record PostRequest(string Text);

  public record EvaluateResponse(float Score);

  public class ApiFagEvaluaterV1 : IFagEvaluater
  {
    private readonly HttpClient _httpClient;

    public string Name { get; set; } = "ApiFagEvaluaterV1";

    public ApiFagEvaluaterV1(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    public async Task<PostEvaluation?> EvaluateAsync(UserPost post, CancellationToken token = default)
    {
      try
      {
        var request = new PostRequest(post.Text);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //var response = await _httpClient.PostAsync("/evaluate", content, token);
        //
        //response.EnsureSuccessStatusCode();
        //
        //var responseJson = await response.Content.ReadAsStringAsync(token);
        //var result = JsonSerializer.Deserialize<EvaluateResponse>(responseJson,
        //    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        float trueScore =  0f;//result?.Score ??


        return new PostEvaluation
        {
          PostId = post.Id,
          Score = trueScore,
          Emotion = trueScore < -0.25 ? Emotion.Negative : trueScore > 0.25 ? Emotion.Positive : Emotion.Neutral,
          Evaluator = Name,
          Date = DateTime.UtcNow
        };
      }
      catch (HttpRequestException ex)
      {
        Console.WriteLine($"HTTP error evaluating post {post.Id}: {ex.Message}");
        return null;
      }
      catch (JsonException ex)
      {
        Console.WriteLine($"JSON parsing error for post {post.Id}: {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Unexpected error evaluating post {post.Id}: {ex.Message}");
        return null;
      }
    }
  }

}
