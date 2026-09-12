FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ApiGateway.Api/ApiGateway.Api.csproj ApiGateway.Api/
RUN dotnet restore ApiGateway.Api/ApiGateway.Api.csproj
COPY . .
RUN dotnet publish ApiGateway.Api/ApiGateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.Api.dll"]
