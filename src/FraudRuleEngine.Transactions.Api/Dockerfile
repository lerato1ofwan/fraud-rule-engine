FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FraudRuleEngine.Transactions.Api/FraudRuleEngine.Transactions.Api.csproj", "src/FraudRuleEngine.Transactions.Api/"]
COPY ["src/FraudRuleEngine.Shared/FraudRuleEngine.Shared.csproj", "src/FraudRuleEngine.Shared/"]
RUN dotnet restore "src/FraudRuleEngine.Transactions.Api/FraudRuleEngine.Transactions.Api.csproj"
COPY . .
WORKDIR "/src/src/FraudRuleEngine.Transactions.Api"
RUN dotnet build "FraudRuleEngine.Transactions.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FraudRuleEngine.Transactions.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudRuleEngine.Transactions.Api.dll"]

