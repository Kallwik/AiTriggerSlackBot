# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Secrets come from environment variables at runtime (Slack__BotToken, etc.)
# -- see README "Storing secrets safely"
ENTRYPOINT ["dotnet", "AiTriggerSlackBot.dll"]
