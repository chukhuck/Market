using FaG.Data.DAL;
using FaG.ML;
using FaG.ML.Services;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
  options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<SentimentModelBuilder>();
builder.Services.AddSingleton<ISentimentAnalysisService, SentimentAnalysisService>(sp =>
{
  var builder = sp.GetRequiredService<SentimentModelBuilder>();
  var service = new SentimentAnalysisService(builder);
  service.InitializeModel("./Data/onnx/SentimentModel.zip");
  return service;
});

var app = builder.Build();

app.MapPost("/evaluate", async (PostRequest request) =>
{

  var service = app.Services.GetRequiredService<ISentimentAnalysisService>();
  var pred = await service.AnalyzeSentimentAsync(request.Text);
  return Results.Json(new EvaluateResponse(pred.Score));
});

app.MapGet("/test", () =>
{
  return "test";
});

app.MapGet("/", () => Results.Ok("FaG.EmotionApi is running"));

app.Run();

public record PostRequest(string Text);
public record EvaluateResponse(float prediction);
