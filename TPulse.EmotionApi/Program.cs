using Microsoft.AspNetCore.Http.Json;
using TPulse.Data;

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

app.Run();

public record PostRequest(string Text);
public record EvaluateResponse(Emotion Emotion);
