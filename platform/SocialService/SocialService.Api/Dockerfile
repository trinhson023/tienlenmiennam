FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore SocialService.Tests/SocialService.Tests.csproj
RUN dotnet test SocialService.Tests/SocialService.Tests.csproj -c Release --no-restore
RUN dotnet restore SocialService.Api/SocialService.Api.csproj
RUN dotnet publish SocialService.Api/SocialService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SocialService.Api.dll"]
