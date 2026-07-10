using AiTriggerSlackBot.Services;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

namespace AiTriggerSlackBot.Handlers;

/// <summary>
/// Handles submission of the "ai_trigger_submit" modal: validates input,
/// calls GitHub's workflow_dispatch API, and DMs the user a confirmation.
/// </summary>
public class AiTriggerViewSubmissionHandler : IViewSubmissionHandler
{
    private readonly ISlackApiClient _slack;
    private readonly GitHubService _gitHub;

    public AiTriggerViewSubmissionHandler(ISlackApiClient slack, GitHubService gitHub)
    {
        _slack = slack;
        _gitHub = gitHub;
    }

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        var values = viewSubmission.View.State.Values;

        var repoName = values["repo_name_block"]["repo_name"].Value?.Trim();
        var ciWorkflowName = values["ci_workflow_name_block"]["ci_workflow_name"].Value?.Trim();
        var ciFileName = values["ci_file_name_block"]["ci_file_name"].Value?.Trim();
        var targetBranch = values["target_branch_block"]["target_branch"].Value?.Trim();
        if (string.IsNullOrWhiteSpace(targetBranch))
            targetBranch = "main";

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(repoName))
            errors["repo_name_block"] = "Repository name is required";
        if (string.IsNullOrWhiteSpace(ciWorkflowName))
            errors["ci_workflow_name_block"] = "CI workflow name is required";
        if (string.IsNullOrWhiteSpace(ciFileName))
            errors["ci_file_name_block"] = "CI file name is required";

        if (errors.Count > 0)
        {
            return ViewSubmissionResponse.Errors(errors);
        }

        // Fire the GitHub call after acking, so the modal closes immediately.
        _ = Task.Run(async () =>
        {
            var (success, error) = await _gitHub.DispatchAsync(
                repoName!, ciWorkflowName!, ciFileName!, targetBranch);

            var text = success
                ? $":white_check_mark: Triggered *Setup AI Trigger* for repo *{repoName}*\n"
                  + $"• CI workflow: `{ciWorkflowName}`\n"
                  + $"• CI file: `{ciFileName}`\n"
                  + $"• Target branch: `{targetBranch}`\n\n"
                  + $"Check progress at: {_gitHub.ActionsUrl}"
                : $":x: Failed to trigger workflow. {error}";

            await _slack.Chat.PostMessage(new Message
            {
                Channel = viewSubmission.User.Id, // DM the user
                Text = text,
            });
        });

        return ViewSubmissionResponse.Null;
    }
}
