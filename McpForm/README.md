# WinForms MCP Designer — AI-driven Windows Forms layout via chat

This solution demonstrates how to drive a Windows Forms UI designer through natural language using Microsoft Semantic Kernel (SK) function calling. The LLM chats with the user and invokes strongly-typed .NET “tools” to add/move/resize controls and draw primitives on a live WinForms surface.

## Goals

- Provide a conversational interface to build and edit a WinForms layout in real time.
- Expose safe, typed operations (add control, update property, draw line/circle, resize) the LLM can call via SK plugins.
- Maintain chat context and a consistent system prompt to keep the assistant on task.

## Components and libraries

Projects

- AIDrawingModule (class library)
	- `DrawerChatService` + `IDrawerChatService`: Orchestrates LLM chat using SK. Builds/uses `ChatHistory`, injects a system message, and enables function-calling via SK. Persists conversation state.
	- `ServiceCollectionExtensions`: Wires up AIDrawingModule options and SK kernels from configuration. Registers plugins and kernels into DI; adds Azure OpenAI or OpenAI chat completion based on model settings.
	- `Settings` (options/config types): `AIDrawingModuleOptions`, `SemanticKernelsSettings`, `KernelSettings`, `Model`, `OpenAISpecificSettings`, `KernelWrapper`. Validated by `SemanticKernelOptionsValidation`.
	- `ConversationRepository` + `IConversationRepository`: Persists `ChatHistory` to disk (per-conversation JSON in the temp folder) and loads it back.
	- `TemplatesProvider` + `ITemplatesProvider`: Loads embedded prompt templates; used to inject the system message (see `AIDrawingModule/Templates/system-message-1.md`).
	- `SKPlugins.SemanticKernelPlugins`: The SK tool surface. Exposes kernel functions the LLM can call: `add_control_to_form`, `update_control_property`, `get_form_layout`, `draw_line`, `draw_circle`, `resize_control`. Each delegates to the `ICommandExecutor` implementation.

- WinFormsApp1 (WinForms app)
	- `Program.cs`: Creates a generic host, binds `AIDrawingModuleOptions` from `appsettings.json`/secrets/env, registers AIDrawing services, and starts the UI.
	- `Form1`: Chat UI window. Hosts the conversation and sends user prompts to `IDrawerChatService`.
	- `LLMDrivenForm`: The live design surface the LLM edits.
	- `CommandExecutor` (implements `ICommandExecutor`): Executes tool calls on the WinForms UI thread (adds/updates controls, draws primitives, resizes). Returns the current layout as JSON.
	- `FormExtensions`: Serializes the current form layout (control tree and properties) into a DTO for tool responses.
	- `appsettings.json`: Configures kernels (OpenAI/Azure OpenAI), API-key lookup, log levels, the default kernel name, and which plugin(s) to load.

## Execution flow

High level

1) App startup
	- `Program.cs` builds a Host and binds `AIDrawingModuleOptions` from `appsettings.json`, user secrets, env vars, and command-line.
	- `ServiceCollectionExtensions.AddAIModuleOptions` validates options, registers the configured kernels, and keys/loads SK plugins declared in `KernelSettings.Plugins`.
	- Each `KernelSettings` produces a `KernelWrapper` with a fully built SK `Kernel`. For Azure OpenAI or OpenAI, the respective chat completion service is added.
	- The app resolves `IDrawerChatService` and starts `Form1` (chat) and `LLMDrivenForm` (canvas).

2) User chats
	- On send, `Form1` calls `DrawerChatService.GetResponse(conversationId, prompt)`.
	- `DrawerChatService` retrieves `ChatHistory` from `ConversationRepository` (temp-folder JSON). If empty, it injects the system message from `TemplatesProvider` (`system-message-1.md`).
	- It configures `PromptExecutionSettings` (temperature, reasoning effort, and `FunctionChoiceBehavior.Auto` with strict schema adherence) and calls SK’s `IChatCompletionService`.

3) Function calling (tool use)
	- When the LLM decides to act, SK invokes functions exposed by `SemanticKernelPlugins`.
	- Each function calls `ICommandExecutor` (implemented by `WinFormsApp1.CommandExecutor`) to mutate the live `LLMDrivenForm` on the UI thread. Examples:
		- Add a control at a given position and name.
		- Update a control property (e.g., `Text`, `Left`, `Top`, `BackColor`/`ForeColor` in HTML color strings).
		- Draw a line or circle overlay.
		- Resize an existing control by deltas.
	- After each action, `CommandExecutor` returns the updated layout via `FormExtensions.SerializeControl()` (JSON), which is sent back to the model and displayed in the chat.

4) Persistence and continuity
	- `DrawerChatService` appends user and assistant messages to `ChatHistory` and persists it via `ConversationRepository` so the conversation (and state) continues across turns per `conversationId`.

Configuration highlights

- `AIDrawingModuleOptions:KernelName` selects which configured kernel to use at runtime.
- Kernels are defined under `AIDrawingModuleOptions:SemanticKernelsSettings:KernelSettings` with:
	- Model provider (`OpenAi` or `AzureOpenAi`), deployment/model name, optional URL, API key name.
	- Plugin list (e.g., `"AIDrawingModule.SKPlugins.SemanticKernelPlugins"`).
	- Temperature and optional reasoning effort.
- API keys are looked up in `AIDrawingModuleOptions:SemanticKernelsSettings:ApiKeys` by name; actual secrets can be overridden by user secrets or environment variables.

System prompt

- `AIDrawingModule/Templates/system-message-1.md` anchors the assistant behavior (e.g., interpret color values as HTML for `ForeColor`/`BackColor`, stick to supported tool parameters, and treat “form”/“control” as the current ones).

## Notes

- The IoC conventions auto-register classes by lifetime marker interfaces. For example, `DrawerChatService`, `TemplatesProvider`, and `ConversationRepository` are singletons.
- `FunctionChoiceBehavior.Auto` lets the LLM call tools as needed while SK enforces typed parameters.
- `ConversationRepository` stores chat history in the system temp directory as `<conversationId>.json`.

## How to run

Prerequisites

- Windows
- .NET 9 SDK
- An API key for either OpenAI or Azure OpenAI

Configure API keys

- Option 1 — Environment variables (works for all shells)

```bash
export AIDrawingModuleOptions__SemanticKernelsSettings__ApiKeys__DevOpenAiApiKey="sk-your-openai-key"
export AIDrawingModuleOptions__SemanticKernelsSettings__ApiKeys__DevAzureOpenAiApiKey="your-azure-openai-key"
```

- Option 2 — .NET user-secrets (scoped to this repo)

```bash
# From repo root
dotnet user-secrets set \
	AIDrawingModuleOptions:SemanticKernelsSettings:ApiKeys:DevOpenAiApiKey \
	"sk-your-openai-key" \
	--project "WinFormsApp1"

dotnet user-secrets set \
	AIDrawingModuleOptions:SemanticKernelsSettings:ApiKeys:DevAzureOpenAiApiKey \
	"your-azure-openai-key" \
	--project "WinFormsApp1"
```

Choose a kernel

- Edit `WinFormsApp1/appsettings.json` and set `AIDrawingModuleOptions:KernelName` to one of the configured kernels, for example:
	- `KernelOpenAi-gpt-5-mini` (OpenAI)
	- `KernelAzureOpenAi-gpt-5-mini` (Azure OpenAI)

Build and run

```bash
dotnet build
dotnet run --project "WinFormsApp1"
```

Try it

- Two windows open: the chat (`Form1`) and the live canvas (`LLMDrivenForm`).
- Sample prompts:
	- "Add a Button named btnOk at 100,100 with text OK"
	- "Set btnOk ForeColor to #ff0000"
	- "Resize btnOk by width +20 and height +10"
	- "Draw a blue line from 10,10 to 200,10"

Troubleshooting
- If the model can’t call tools, ensure the `Plugins` array in `appsettings.json` includes `AIDrawingModule.SKPlugins.SemanticKernelPlugins` for the chosen kernel.
- If you see key/config errors, verify the key names match `ApiKeys` and that `KernelName` matches one of the configured `KernelSettings:Name` values.
- Azure OpenAI requires a non-empty `Url`; OpenAI sets `RequiresUrl=false`.

