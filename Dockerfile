# Multi-project Dockerfile for development
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore

WORKDIR /src/TPulse.EmotionApi
RUN dotnet publish -c Release -o /app/emotionapi

WORKDIR /src/TPulse.Scheduler
RUN dotnet publish -c Release -o /app/scheduler

WORKDIR /src/TPulse.Web
RUN dotnet publish -c Release -o /app/web

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app /app
EXPOSE 5000
CMD ["dotnet", "/app/emotionapi/TPulse.EmotionApi.dll"]