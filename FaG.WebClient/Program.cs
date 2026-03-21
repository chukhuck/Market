using FaG.Dashboard.Services;
using FaG.Data.DAL;
using FaG.WebClient.Components;
using FaG.WebClient.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Строка подключения (такая же, как в scheduler/emotionapi)
var connectionString = Environment.GetEnvironmentVariable("FAG_DB")
    ?? "Host=fagpostgres;Port=5432;Database=fagdb;Username=faguser;Password=fagpassword";//fagpostgres

// Регистрируем существующий DbContext из FaG.Data
builder.Services.AddDbContext<FaGDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрируем сервис статистики
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
