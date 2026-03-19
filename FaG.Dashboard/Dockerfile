# Этап сборки с SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .

# Восстанавливаем зависимости только для проекта Dashboard (чтобы не собирать всё решение)
RUN dotnet restore "FaG.Dashboard/FaG.Dashboard.csproj"

# Собираем проект в режиме Release
RUN dotnet build "FaG.Dashboard/FaG.Dashboard.csproj" -c Release -o /app/build

COPY FaG.Dashboard/wwwroot /app/build/wwwroot

# Этап публикации
FROM build AS publish
RUN dotnet publish "FaG.Dashboard/FaG.Dashboard.csproj" -c Release -o /app/publish

# Финальный этап — минимальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Открываем порт (Blazor Server по умолчанию использует порт 80)
EXPOSE 80

# Точка входа приложения
ENTRYPOINT ["dotnet", "FaG.Dashboard.dll"]