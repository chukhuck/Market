using FaG.Dashboard.Components;
using FaG.Dashboard.Services;
using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://+:80");
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Строка подключения (такая же, как в scheduler/emotionapi)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=fagpostgres;Port=5432;Database=fagdb;Username=faguser;Password=fagpassword";

// Регистрируем существующий DbContext из FaG.Data
builder.Services.AddDbContext<FaGDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрируем сервис статистики
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

