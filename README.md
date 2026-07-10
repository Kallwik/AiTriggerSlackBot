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

## No local .NET install? Use these instead

### A. Write/test code in the browser — GitHub Codespaces
This repo includes a `.devcontainer/devcontainer.json` that gives you a full
VS Code environment with .NET 8 pre-installed, running entirely in the
browser (or connected from VS Code Desktop to a remote container) — nothing
touches your laptop.

1. On the repo page on GitHub: **Code** button → **Codespaces** tab → **Create codespace on main**
2. Wait for it to build (first time takes a minute or two)
3. A browser-based VS Code opens with a terminal already inside the container
4. Set your secrets and run:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Slack:BotToken" "xoxb-..."
   dotnet user-secrets set "Slack:AppLevelToken" "xapp-..."
   dotnet user-secrets set "GitHub:PersonalAccessToken" "ghp_..."
   dotnet run
   ```
   Socket Mode connects fine from inside a Codespace, so `/run-ai-trigger` in
   Slack works immediately for testing — but the Codespace stops when you
   close it or it idles out, so it's not meant for 24/7 hosting (see below
   for that).
5. GitHub gives a free monthly quota of Codespaces hours; check
   **github.com/settings/billing** if you want to confirm your usage.

### B. Run it 24/7 — deploy to Railway or Render
Both platforms build directly from your GitHub repo using the included
`Dockerfile` — you push code, they build and run it in the cloud. No install
on your machine at any point.

**Railway (https://railway.app):**
1. Sign in with GitHub → **New Project** → **Deploy from GitHub repo** → pick `AiTriggerSlackBot`
2. Railway detects the `Dockerfile` automatically and builds it
3. Go to the service's **Variables** tab and add:
   `Slack__BotToken`, `Slack__AppLevelToken`, `GitHub__PersonalAccessToken`,
   `GitHub__Owner`, `GitHub__CentralRepo`, `GitHub__WorkflowFile`, `GitHub__WorkflowRef`
4. Deploy — since this is a background worker (not a web server), you don't
   need to expose a port; just let it run.

**Render (https://render.com):**
1. **New** → **Web Service** (or **Background Worker** if offered on your plan) → connect the repo
2. Render detects the `Dockerfile`
3. Add the same environment variables under **Environment**
4. Deploy

Either way, once it's live you'll see the "⚡️ AI Trigger Slack bot is running"
line in the platform's logs, and `/run-ai-trigger` will work from Slack same
as running it locally.

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

## 3. Configure — storing secrets safely
`appsettings.json` in this repo is a **template only** — the token fields are
left blank on purpose and it's meant to be committed as-is. Never fill in
real tokens there; GitHub's push protection will (correctly) block a push
that contains a live Slack/GitHub token, and if you ever do commit one,
treat it as compromised and rotate it immediately.

Use one of these instead, from least to most involved:

**Option A — dotnet user-secrets (recommended for local dev)**
Stores values outside the repo folder entirely, so they can't be committed:
```bash
cd AiTriggerSlackBot
dotnet user-secrets init          # already configured via <UserSecretsId> in the .csproj
dotnet user-secrets set "Slack:BotToken" "xoxb-..."
dotnet user-secrets set "Slack:AppLevelToken" "xapp-..."
dotnet user-secrets set "GitHub:PersonalAccessToken" "ghp_..."
```

**Option B — environment variables** (good for servers/containers/CI)
.NET config maps `__` to `:` automatically:
```bash
export Slack__BotToken="xoxb-..."
export Slack__AppLevelToken="xapp-..."
export GitHub__PersonalAccessToken="ghp_..."
```

Both are picked up automatically by `Program.cs` and override the blank
values in `appsettings.json`.

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
