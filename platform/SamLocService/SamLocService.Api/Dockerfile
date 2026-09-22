FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore SamLocService.Tests/SamLocService.Tests.csproj
RUN dotnet test SamLocService.Tests/SamLocService.Tests.csproj -c Release --no-restore
RUN dotnet restore SamLocService.Api/SamLocService.Api.csproj
RUN dotnet publish SamLocService.Api/SamLocService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SamLocService.Api.dll"]
