FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FraudRuleEngine.Reporting.Api/FraudRuleEngine.Reporting.Api.csproj", "src/FraudRuleEngine.Reporting.Api/"]
COPY ["src/FraudRuleEngine.Shared/FraudRuleEngine.Shared.csproj", "src/FraudRuleEngine.Shared/"]
RUN dotnet restore "src/FraudRuleEngine.Reporting.Api/FraudRuleEngine.Reporting.Api.csproj"
COPY . .
WORKDIR "/src/src/FraudRuleEngine.Reporting.Api"
RUN dotnet build "FraudRuleEngine.Reporting.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FraudRuleEngine.Reporting.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudRuleEngine.Reporting.Api.dll"]

