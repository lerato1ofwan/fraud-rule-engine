FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FraudRuleEngine.Evaluations.Worker/FraudRuleEngine.Evaluations.Worker.csproj", "src/FraudRuleEngine.Evaluations.Worker/"]
COPY ["src/FraudRuleEngine.Shared/FraudRuleEngine.Shared.csproj", "src/FraudRuleEngine.Shared/"]
COPY ["src/FraudRuleEngine.Core/FraudRuleEngine.Core.csproj", "src/FraudRuleEngine.Core/"]
RUN dotnet restore "src/FraudRuleEngine.Evaluations.Worker/FraudRuleEngine.Evaluations.Worker.csproj"
COPY . .
WORKDIR "/src/src/FraudRuleEngine.Evaluations.Worker"
RUN dotnet build "FraudRuleEngine.Evaluations.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FraudRuleEngine.Evaluations.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudRuleEngine.Evaluations.Worker.dll"]

