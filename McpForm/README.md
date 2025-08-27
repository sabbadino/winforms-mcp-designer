# WinForms MCP Designer

An LLM-driven WinForms layout editor. Build and modify a live Windows Forms UI by chatting. The LLM calls MCP tools you expose to add/move/resize controls and draw on a second form in real time.

## What this project does

- Hosts an in-process MCP server (ASP.NET Core minimal host) that exposes UI-manipulation tools.
- Starts a WinForms app with two windows:
  - Form1: a simple chat UI where you type requests.
  - LLMDrivenForm: a live canvas the tools modify (add controls, resize, draw).
- Bridges OpenAI Chat function-calling with MCP tools:
  - Lists MCP tools and registers them as OpenAI “function tools”.
  - Runs a tool-call loop: on ToolCalls the client invokes the MCP tool, feeds results back to the model, and continues until a final answer.
- Logs every conversation to timestamped JSON files.

## How it works (high level)

- Program.cs
  - Builds an ASP.NET Core WebApplication inside the WinForms process.
  - Registers MCP server and auto-discovers tools from the current assembly.
  - Maps the MCP endpoint at /mcp and runs the web host in the background.
  - Creates an OpenAI ChatClient from the configured model and API key.
  - Creates an IMcpClient using SSE transport pointing at the configured MCP server URL.
  - Boots the WinForms app passing IMcpClient, ChatClient, and IConfiguration to Form1.

- Form1.cs
  - Maintains per-conversation chat history and a serializable log.
  - On Send:
    - Loads tool definitions from IMcpClient and adds them to ChatCompletionOptions as OpenAI function tools.
    - Calls ChatClient.CompleteChat in a loop:
      - If ToolCalls: invoke each tool via IMcpClient.CallToolAsync and append ToolChatMessages, then continue.
      - If Stop: append the assistant response and render it in the ListBox.
  - Shows the LLMDrivenForm next to the chat to visualize changes.
  - Persists a JSON log per conversation under the working directory.

- Tools/McpTools.cs
  - Annotated with [McpServerToolType] and [McpServerTool] so they’re discovered by the MCP server.
  - Uses Control/Graphics APIs to update LLMDrivenForm on the UI thread via Control.Invoke.

- Bridging helpers
  - McpExtensions.cs converts MCP tool schemas to OpenAI ChatTool definitions.
  - FormExtensions.cs serializes the live form/control tree to a simple POCO layout (for tool return values and logging).

## Solution structure

- McpForm.sln — Solution file
- WinFormsApp1/
  - Program.cs — Web host + MCP server + Chat/OpenAI wiring + WinForms bootstrap
  - Form1.cs / Form1.Designer.cs / Form1.resx — Chat UI and tool-call loop
  - LLMDrivenForm.cs / LLMDrivenForm.Designer.cs / LLMDrivenForm.resx — Live form that tools modify
  - Tools/McpTools.cs — MCP tools (add/update controls, draw primitives, resize)
  - McpExtensions.cs — MCP ➜ OpenAI tool conversion
  - FormExtensions.cs — Serialize control tree to ControlLayout
  - Templates/system-message-1.md — System prompt guiding the LLM’s behavior
  - appsettings.json — App configuration (model, temperature, MCP server URL, logging)
  - Properties/launchSettings.json — Local dev URLs for the web host

## Exposed MCP tools (server side)

- add_control_to_form(controlType, controlText, controlName, controlHorizontalPosition, controlVerticalPosition)
- update_control_property(controlName, propertyName, propertyValue)
  - Supported ControlProperties: Text, Left, Top, BackColor, ForeColor (HTML color strings are parsed)
- resize_control(controlName, WidthDelta, HeightDelta)
- get_form_layout()
- draw_line(startX, startY, endX, endY, color)
- draw_circle(startX, startY, radius, color)

Each tool returns the updated serialized layout (except draw primitives, which also return layout for convenience). Drawing uses Control.CreateGraphics and is not persisted on repaint.

## Dependencies (NuGet)

- Azure.AI.OpenAI (2.3.0-beta.2)
- ModelContextProtocol (0.3.0-preview.4)
- ModelContextProtocol.AspNetCore (0.3.0-preview.4)
- Microsoft.AspNetCore.OpenApi (9.0.8)
- Microsoft.Extensions.Configuration.UserSecrets (9.0.8)

Key namespaces/APIs used:
- OpenAI.OpenAIClient and OpenAI.Chat (chat + tool-calling loop)
- ModelContextProtocol.Client (IMcpClient, SseClientTransport)
- ModelContextProtocol.Server (tool annotations and hosting extensions)
- System.Windows.Forms (Form, Control, Graphics)

## Configuration

appsettings.json (values copied to output on build):

- mcp-server: The HTTP URL of the MCP server to call (e.g., http://localhost:5000/mcp). You can point this to the in-process server if you configure the ASP.NET host to listen there.
- model-name: OpenAI model identifier (example: gpt-4.1)
- temperature: integer temperature (0–2)
- reasoning-effort: optional, e.g., "low" (when supported by the model/SDK)

OpenAI API key is read from user secrets (not in appsettings.json). The project is already configured with a UserSecretsId.

Example: set the secret at the project root (Windows bash):

```bash
# Inside McpForm/WinFormsApp1
dotnet user-secrets set "open-ai-api-key" "<YOUR_OPENAI_API_KEY>"
```

Optional: force the in-process web host to a known URL so the client can target it:

```bash
# Example to host the MCP endpoint at http://localhost:5000
export ASPNETCORE_URLS="http://localhost:5000"
```

Then set appsettings.json:

```json
{
  "mcp-server": "http://localhost:5000/mcp",
  "model-name": "gpt-4.1",
  "temperature": 0,
  "reasoning-effort": null
}
```

## Run it

- Requirements: Windows, .NET 9 SDK.
- From the repo root or project directory:

```bash
# Restore
dotnet restore

# Run the WinForms app
dotnet run --project WinFormsApp1/WinFormsApp1.csproj
```

What you’ll see:
- A chat window (Form1) and a separate LLMDrivenForm window.
- Type natural language requests like “Add a blue Button named Submit at x=100,y=120” or “Increase the width of Submit by 40”. The model will call the appropriate MCP tools to perform the change.

Conversation logs are saved to timestamped JSON files in the working directory.

## Prompting behavior

The system prompt lives in `WinFormsApp1/Templates/system-message-1.md`. It reminds the model to:
- Treat “form” as the currently edited live form.
- Provide HTML color strings when setting ForeColor/BackColor.
- Only use supported tool parameters (don’t invent properties the tools don’t accept).

You can tweak this file to adjust behavior and capabilities.

## Extending the toolset

1. Add a method to `Tools/McpTools.cs` and decorate it with `[McpServerTool(Name = "your_tool")]`.
2. Use `[Description("...")]` on parameters for better schema/help text.
3. Keep UI updates on the UI thread: call `Form1._LLMDrivenForm.Invoke(...)`.
4. Return a useful payload (often the serialized form layout).

The server is registered with `.WithToolsFromAssembly()`, so tools are auto-discovered at startup.

## Notes and limitations

- `UpdateControlProperty` enforces type checks at runtime and currently supports a limited set of properties.
- Drawing uses `CreateGraphics` and won’t persist after a repaint; consider custom controls or painting in OnPaint if persistence is needed.
- Control instantiation assumes types under `System.Windows.Forms` (e.g., Button, Label, TextBox, ...).
- If you want the in-process MCP server and client to talk to each other, ensure `ASPNETCORE_URLS` (or equivalent host config) matches `mcp-server` in appsettings.

## IMPORTANT CONSIDERATIONS

- Having communication with tools via http, hosting in the same app both the mcp client and the mcp http server offer flexibility but it might be overkill and not required. 
One might consider to expose  the tools as local (using Semantic Kernel), so tehre is no need to make a network hop and expose and http endpoint from the client app.
- When using gpt5 reasoning family, using reasoning_effort = minimum is sufficent but the current version of SDK does not allow to set it (so we are currently setting it to "low"): switch to "minimum" when updated sdk is available.