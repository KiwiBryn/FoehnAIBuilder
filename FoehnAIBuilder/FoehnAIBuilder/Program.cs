using FoehnAIBuilder.Configuration;
using FoehnAIBuilder.Plugins;
using FoehnAIBuilder.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

var builder = Host.CreateApplicationBuilder(args);

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
   Console.WriteLine("  dotnet user-secrets set \"Mistral:ApiKey\" \"<your-key>\" --project FoehnSharpV1");
   return 1;
}

// Resolving the registry triggers plugin discovery/loading.
var toolRegistry = host.Services.GetRequiredService<IToolRegistry>();
var session = host.Services.GetRequiredService<AgentSession>();

Console.WriteLine();
Console.WriteLine($"Working folder: {Environment.CurrentDirectory}");
Console.WriteLine($"{toolRegistry.Tools.Count} tool(s) loaded: {string.Join(", ", toolRegistry.Tools.Select(t => t.Name))}");
Console.WriteLine("Commands: /quit, /context, /clear, /help");
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
      if (HandleCommand(input, session))
         break;
      continue;
   }

   try
   {
      Console.WriteLine("-> Sending message...");
      var reply = await session.SendAsync(input, cts.Token);
      Console.WriteLine("-> Message processed.");
      Console.WriteLine();
      Console.WriteLine($"FoehnSharp: {reply}");
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
bool HandleCommand(string command, AgentSession agentSession)
{
   switch (command.ToLowerInvariant())
   {
      case "/quit":
         return true;

      case "/context":
         PrintContext(agentSession.History);
         return false;

      case "/clear":
         agentSession.ClearContext();
         Console.WriteLine("Context cleared (system prompt kept).");
         return false;

      default:
         Console.WriteLine($"Unknown command: {command}. Available commands: /quit, /context, /clear");
         return false;
   }
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
