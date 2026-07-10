using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

namespace AiTriggerSlackBot.Handlers;

/// <summary>
/// Handles the /run-ai-trigger slash command by opening a modal
/// that collects: Repository name, CI workflow name, CI file name, Target branch.
/// </summary>
public class AiTriggerSlashCommandHandler : ISlashCommandHandler
{
    private readonly ISlackApiClient _slack;

    public AiTriggerSlashCommandHandler(ISlackApiClient slack)
    {
        _slack = slack;
    }

    public async Task Handle(SlashCommand command)
    {
        var modal = new ModalViewDefinition
        {
            CallbackId = "ai_trigger_submit",
            Title = "Setup AI Trigger",
            Submit = "Run workflow",
            Close = "Cancel",
            Blocks = new List<Block>
            {
                new InputBlock
                {
                    BlockId = "repo_name_block",
                    Label = "Repository name",
                    Element = new PlainTextInput
                    {
                        ActionId = "repo_name",
                        Placeholder = "e.g. Test-repo",
                    },
                },
                new InputBlock
                {
                    BlockId = "ci_workflow_name_block",
                    Label = "CI workflow name",
                    Element = new PlainTextInput
                    {
                        ActionId = "ci_workflow_name",
                        Placeholder = "e.g. CI",
                    },
                },
                new InputBlock
                {
                    BlockId = "ci_file_name_block",
                    Label = "CI file name",
                    Element = new PlainTextInput
                    {
                        ActionId = "ci_file_name",
                        Placeholder = "e.g. ci.yml",
                    },
                },
                new InputBlock
                {
                    BlockId = "target_branch_block",
                    Label = "Target branch",
                    Optional = true,
                    Element = new PlainTextInput
                    {
                        ActionId = "target_branch",
                        Placeholder = "main (default)",
                    },
                    Hint = "Branch to watch for CI failures; AI_FIX is created from this branch.",
                },
            },
        };

        await _slack.Views.Open(command.TriggerId, modal);
    }
}
