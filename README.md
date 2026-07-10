# AI Trigger Slack Bot (C# / .NET 8)

Same bot as the Node version, in C#: a slash command opens a modal for the
4 parameters, and on submit it calls GitHub's `workflow_dispatch` API to run
your `Setup AI Trigger` workflow. Built with [SlackNet](https://github.com/soxtoby/SlackNet)
using Socket Mode, so no public URL is needed to get started.

## Project layout
```
AiTriggerSlackBot/
├── AiTriggerSlackBot.csproj
├── Program.cs                              # wires everything up, connects socket mode
├── appsettings.json                        # config template (tokens go here or in env vars)
├── Handlers/
│   ├── AiTriggerSlashCommandHandler.cs      # opens the modal
│   └── AiTriggerViewSubmissionHandler.cs    # validates + dispatches on submit
└── Services/
    └── GitHubService.cs                     # calls the GitHub REST API
```

## 1. Slack app setup
Same as the Node version:
1. https://api.slack.com/apps → **Create New App** → **From scratch**
2. **Socket Mode** → toggle On → generate an App-Level Token with `connections:write` → this is `Slack:AppLevelToken`
3. **Slash Commands** → **Create New Command** → `/run-ai-trigger` (leave Request URL blank)
4. **Interactivity & Shortcuts** → toggle On (Request URL blank)
5. **OAuth & Permissions** → Bot Token Scopes → add `commands`, `chat:write`
6. **Install to Workspace** → copy the Bot User OAuth Token (`xoxb-...`) → this is `Slack:BotToken`

## 2. GitHub token
A PAT with `repo` + `workflow` scopes → `GitHub:PersonalAccessToken`.

## 3. Configure
Edit `appsettings.json` directly, **or** (recommended so you don't commit secrets)
override via environment variables using .NET's `__` convention:
```bash
export Slack__BotToken="xoxb-..."
export Slack__AppLevelToken="xapp-..."
export GitHub__PersonalAccessToken="ghp_..."
export GitHub__Owner="Kallwik"
export GitHub__CentralRepo="ai-workflows"
export GitHub__WorkflowFile="setup-ai-trigger.yml"
export GitHub__WorkflowRef="main"
```
Env vars override whatever's in `appsettings.json`.

## 4. Run it
```bash
dotnet restore
dotnet run
```
You should see: `⚡️ AI Trigger Slack bot is running (command: /run-ai-trigger)`.

## 5. Use it
Invite the bot to a channel (`/invite @YourBotName`), then run `/run-ai-trigger`.
Fill in the modal → **Run workflow**. You'll get a DM confirming the dispatch.

## Note on the SlackNet API surface
SlackNet's exact method/handler signatures (`RegisterSlashCommandHandler`,
`ISlashCommandHandler`, `IViewSubmissionHandler`, block classes like
`InputBlock`/`PlainTextInput`) can shift slightly between versions. This code
targets the pattern shown in SlackNet's own docs and demo project. If
`dotnet build` flags a signature mismatch after `dotnet restore` pulls the
package, check the installed version's **SlackNetDemo** example on GitHub
(https://github.com/soxtoby/SlackNet/tree/master/Examples/SlackNetDemo) and
adjust the handler method signature/registration call to match — the overall
structure (slash command → open modal → view submission → call GitHub) stays
the same.

## Keeping it running
Like the Node version, this holds an open Socket Mode connection, so run it
as a long-lived process — a systemd service, Docker container, or Windows
Service, not something that sleeps/scales to zero.
