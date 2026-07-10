using AiTriggerSlackBot.Handlers;
using AiTriggerSlackBot.Services;
using Microsoft.Extensions.Configuration;
using SlackNet;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    // Override any value via env vars, e.g. Slack__BotToken, GitHub__PersonalAccessToken
    .AddEnvironmentVariables()
    .Build();

string RequireConfig(string key) =>
    config[key] ?? throw new InvalidOperationException($"Missing configuration value: {key}");

var botToken = RequireConfig("Slack:BotToken");
var appLevelToken = RequireConfig("Slack:AppLevelToken");

var gitHubOptions = new GitHubOptions(
    PersonalAccessToken: RequireConfig("GitHub:PersonalAccessToken"),
    Owner: RequireConfig("GitHub:Owner"),
    CentralRepo: RequireConfig("GitHub:CentralRepo"),
    WorkflowFile: RequireConfig("GitHub:WorkflowFile"),
    WorkflowRef: config["GitHub:WorkflowRef"] ?? "main");

var gitHubService = new GitHubService(new HttpClient(), gitHubOptions);

var builder = new SlackServiceBuilder()
    .UseApiToken(botToken)
    .UseAppLevelToken(appLevelToken);

var apiClient = builder.GetApiClient();

var client = builder
    .RegisterSlashCommandHandler("/run-ai-trigger", new AiTriggerSlashCommandHandler(apiClient))
    .RegisterInteractionHandler(new AiTriggerViewSubmissionHandler(apiClient, gitHubService))
    .GetSocketModeClient();

await client.Connect();

Console.WriteLine("⚡️ AI Trigger Slack bot is running (command: /run-ai-trigger). Press Ctrl+C to exit.");

// Keep the process alive until cancelled.
var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    tcs.SetResult();
};
await tcs.Task;
