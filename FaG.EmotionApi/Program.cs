using Microsoft.AspNetCore.Http.Json;
using FaG.Data.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

app.MapPost("/evaluate", (PostRequest request) =>
{
    // Заглушка: всегда возвращаем Emotion.None
    return Results.Json(new EvaluateResponse ( Emotion.None ));
});

app.MapGet("/test", () =>
{
  return "test";
});

app.MapGet("/", () => Results.Ok("FaG.EmotionApi is running"));

app.Run();

public record PostRequest(string Text);
public record EvaluateResponse(Emotion Emotion);
