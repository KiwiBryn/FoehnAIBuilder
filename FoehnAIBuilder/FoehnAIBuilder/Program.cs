// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Configuration;
using FoehnAIBuilder.Plugins;
using FoehnAIBuilder.Chat;
using MistralAI.Client;
using MistralAI.Client.DTOs.Shared;

const string SouthernAlps20x20 = @" 
        /\       
     /\_/ \      /\ 
    /  \   \    /  \
   / /\ \___\  / /\ \
  /_/  \     \/  \  \
 /  \   \     \   \  \
/ /\ \___\   / \___\ \
/_/  \     \_/      \_\ 
devMobile Software NZ © 2026-08";

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
   Args = args,
   ContentRootPath = AppContext.BaseDirectory,
});

// Public settings live in appsettings.json; the Mistral API key is kept out of source
// control in .NET User Secrets (dotnet user-secrets set "Mistral:ApiKey" "...").
builder.Configuration.AddUserSecrets<FoehnAIBuilder.AssemblyMarker>(optional: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient();
builder.Services.Configure<MistralAiOptions>(builder.Configuration.GetSection("Mistral"));
builder.Services.Configure<FoehnAIBuilderOptions>(builder.Configuration.GetSection("FoehnAIBuilder"));

builder.Services.AddTransient<ChatCompletionClient>();
builder.Services.AddSingleton<IToolPluginLoader, ToolPluginLoader>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddSingleton<ISystemPromptProvider, FileSystemPromptProvider>();
builder.Services.AddSingleton<AgentSession>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var mistralOptions = host.Services.GetRequiredService<IOptions<MistralAiOptions>>().Value;
var foehnAIBuilderOptions = host.Services.GetRequiredService<IOptions<FoehnAIBuilderOptions>>().Value;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine(SouthernAlps20x20);
Console.WriteLine($"Build:{Assembly.GetEntryAssembly()?.GetName().Version?.ToString()} MistralAI model:{mistralOptions.DefaultModel}");

if (!string.IsNullOrWhiteSpace(foehnAIBuilderOptions.WorkingDirectory))
{
   if (Directory.Exists(foehnAIBuilderOptions.WorkingDirectory))
   {
      Environment.CurrentDirectory = foehnAIBuilderOptions.WorkingDirectory;
   }
   else
   {
      logger.LogWarning("Configured working directory does not exist: {WorkingDirectory}", foehnAIBuilderOptions.WorkingDirectory);
   }
}

if (string.IsNullOrWhiteSpace(mistralOptions.ApiKey))
{
   Console.WriteLine("No Mistral API key configured.");
   Console.WriteLine("Set one with:");
   Console.WriteLine("  dotnet user-secrets set \"Mistral:ApiKey\" \"<your-key>\" --project FoehnAI");
   return 1;
}

// Resolving the registry triggers plugin discovery/loading.
var toolRegistry = host.Services.GetRequiredService<IToolRegistry>();
var systemPromptProvider = host.Services.GetRequiredService<ISystemPromptProvider>();  
var session = host.Services.GetRequiredService<AgentSession>();

Console.WriteLine();
Console.WriteLine($"System prompt file: {systemPromptProvider.SystemPromptFilename()}");
Console.WriteLine($"Working folder: {Environment.CurrentDirectory}");
Console.WriteLine($"{toolRegistry.Tools.Count} tool(s) loaded: {string.Join(", ", toolRegistry.Tools.Select(t => t.Name))}");

// List available skills
var availableSkills = systemPromptProvider.GetAvailableSkills();
if (availableSkills.Count > 0)
{
   Console.WriteLine($"Available skills: {string.Join(", ", availableSkills.Select(s => $"/{s}"))}");
}

Console.WriteLine("Commands: /quit, /context, /clear, /help, /skills");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
   e.Cancel = true;
   cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
   Console.Write(">");
   var input = Console.ReadLine();
   if (input is null)
      break;

   input = input.Trim();
   if (input.Length == 0)
      continue;

   if (input.StartsWith('/'))
   {
      if (HandleCommand(input, session, systemPromptProvider))
         break;
      continue;
   }

   try
   {
      Console.WriteLine("-> Sending message...");
      var reply = await session.SendAsync(input, cts.Token);
      Console.WriteLine("-> Message processed.");
      Console.WriteLine();
      Console.WriteLine($"FoehnAI: {reply}");
      Console.WriteLine();
   }
   catch (OperationCanceledException)
   {
      Console.WriteLine("Cancelled.");
   }
   catch (MistralAiException ex)
   {
      logger.LogError(ex, "Mistral AI API error");
      Console.WriteLine($"Mistral AI API error: {ex.Message}");
   }
   catch (Exception ex)
   {
      logger.LogError(ex, "Error handling message");
      Console.WriteLine($"Error: {ex.Message}");
   }
}

Console.WriteLine("Goodbye!");
return 0;

// Returns true when the REPL should exit.
bool HandleCommand(string command, AgentSession agentSession, ISystemPromptProvider systemPromptProvider)
{
   if (command.Length == 1)
   {
      Console.WriteLine("Unknown command. Available commands: /quit, /context, /clear, /help, /skills");
      return false;
   }

   var commandParts = command.Substring(1).Split(' ', 2);
   var commandName = commandParts[0].ToLowerInvariant();
   var commandArgument = commandParts.Length > 1 ? commandParts[1] : string.Empty;

   switch (commandName)
   {
      case "quit":
         return true;

      case "context":
         PrintContext(agentSession.History);
         return false;

      case "clear":
         agentSession.ClearContext();
         Console.WriteLine("Context cleared (system prompt kept).");
         return false;

      case "help":
         ShowHelp();
         return false;

      case "skills":
         ShowAvailableSkills(systemPromptProvider);
         return false;

      default:
         // Check if this is a skill command
         var skillContent = systemPromptProvider.LoadSkill(commandName);
         if (skillContent != null)
         {
            Console.WriteLine($"Loading skill: {commandName}");
            Console.WriteLine($"Skill content:");
            Console.WriteLine(skillContent);
            Console.WriteLine();
            return false;
         }
         else
         {
            Console.WriteLine($"Unknown command or skill: {command}. Available commands: /quit, /context, /clear, /help, /skills");
            return false;
         }
   }
}

void ShowHelp()
{
   Console.WriteLine("Available commands:");
   Console.WriteLine("  /quit          - Exit the application");
   Console.WriteLine("  /context       - Show the current conversation context");
   Console.WriteLine("  /clear         - Clear the conversation context (keeps system prompt)");
   Console.WriteLine("  /help          - Show this help message");
   Console.WriteLine("  /skills        - List available skills");
   Console.WriteLine("  /<skillname>   - Load and display a specific skill");
   Console.WriteLine();
}

void ShowAvailableSkills(ISystemPromptProvider systemPromptProvider)
{
   var availableSkills = systemPromptProvider.GetAvailableSkills();
   
   if (availableSkills.Count == 0)
   {
      Console.WriteLine("No skills available.");
      return;
   }

   Console.WriteLine("Available skills (use /<skillname> to load):");
   foreach (var skill in availableSkills)
   {
      Console.WriteLine($"  /{skill}");
   }
   Console.WriteLine();
}

void PrintContext(IReadOnlyList<MessageBase> history)
{
   if (history.Count == 0)
   {
      Console.WriteLine("(no messages)");
      return;
   }

   Console.WriteLine($"--- Context: {history.Count} message(s) ---");
   foreach (var message in history)
   {
      Console.Write($"[{message.Role}]");
      if (message is ToolMessage toolMessage)
         Console.Write($" (tool_call_id: {toolMessage.ToolCallId})");
      Console.WriteLine();

      if (!string.IsNullOrEmpty(message.Content))
         Console.WriteLine(message.Content);

      if (message is AssistantMessage { ToolCalls.Count: > 0 } assistantMessage)
      {
         foreach (var call in assistantMessage.ToolCalls)
            Console.WriteLine($"  tool_call: {call.Function?.Name}({call.Function?.Arguments})");
      }

      Console.WriteLine();
   }
   Console.WriteLine("--- End of context ---");
}
