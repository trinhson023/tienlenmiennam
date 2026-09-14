FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore StatisticsService.Tests/StatisticsService.Tests.csproj
RUN dotnet test StatisticsService.Tests/StatisticsService.Tests.csproj -c Release --no-restore
RUN dotnet restore StatisticsService.Api/StatisticsService.Api.csproj
RUN dotnet publish StatisticsService.Api/StatisticsService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StatisticsService.Api.dll"]
