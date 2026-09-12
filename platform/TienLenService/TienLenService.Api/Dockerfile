FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore TienLenService.Tests/TienLenService.Tests.csproj
RUN dotnet test TienLenService.Tests/TienLenService.Tests.csproj -c Release --no-restore
RUN dotnet restore TienLenService.Api/TienLenService.Api.csproj
RUN dotnet publish TienLenService.Api/TienLenService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TienLenService.Api.dll"]
