using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiTriggerSlackBot.Services;

public record GitHubOptions(
    string PersonalAccessToken,
    string Owner,
    string CentralRepo,
    string WorkflowFile,
    string WorkflowRef);

public class GitHubService
{
    private readonly HttpClient _http;
    private readonly GitHubOptions _options;

    public GitHubService(HttpClient http, GitHubOptions options)
    {
        _http = http;
        _options = options;

        _http.BaseAddress = new Uri("https://api.github.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.PersonalAccessToken);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AiTriggerSlackBot/1.0");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    /// <summary>
    /// Fires a workflow_dispatch event for the "Setup AI Trigger" workflow,
    /// passing the 4 collected parameters as inputs.
    /// </summary>
    public async Task<(bool Success, string? Error)> DispatchAsync(
        string repoName,
        string ciWorkflowName,
        string ciFileName,
        string targetBranch)
    {
        var url = $"repos/{_options.Owner}/{_options.CentralRepo}/actions/workflows/{_options.WorkflowFile}/dispatches";

        // "ref" is what GitHub's API expects as the key name.
        var payload = new Dictionary<string, object>
        {
            ["ref"] = _options.WorkflowRef,
            ["inputs"] = new Dictionary<string, string>
            {
                ["repo_name"] = repoName,
                ["ci_workflow_name"] = ciWorkflowName,
                ["ci_file_name"] = ciFileName,
                ["target_branch"] = targetBranch,
            },
        };

        var response = await _http.PostAsJsonAsync(url, payload);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return (true, null);
        }

        var errorText = await response.Content.ReadAsStringAsync();
        return (false, $"HTTP {(int)response.StatusCode}: {errorText}");
    }

    public string ActionsUrl => $"https://github.com/{_options.Owner}/{_options.CentralRepo}/actions";
}
