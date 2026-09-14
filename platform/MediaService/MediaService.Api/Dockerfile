FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore MediaService.Tests/MediaService.Tests.csproj
RUN dotnet test MediaService.Tests/MediaService.Tests.csproj -c Release --no-restore
RUN dotnet restore MediaService.Api/MediaService.Api.csproj
RUN dotnet publish MediaService.Api/MediaService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaService.Api.dll"]
