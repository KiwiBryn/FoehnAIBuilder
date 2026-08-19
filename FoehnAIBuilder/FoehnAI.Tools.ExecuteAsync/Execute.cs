using System.ComponentModel;
using System.Diagnostics;
using FoehnAIBuilder.Abstractions;
using FoehnAI.Tools.ExecuteAsync;
using Microsoft.Extensions.Logging;

namespace FoehnAIBuilder.Tools.ExecuteAsync;

/// <summary>
/// Launches a command-line program in its own console window and returns immediately,
/// without waiting for it to exit or capturing its output. Intended for interactive or
/// long-running programs (e.g. a REPL, a server, a GUI) that the user will keep using and
/// shut down themselves - unlike <c>execute</c>, which redirects and closes stdin so the
/// child gets instant EOF and is expected to run to completion unattended.
/// </summary>
public sealed class Execute : ITool
{
    private readonly ILogger<Execute> _logger;

    public Execute(ILogger<Execute> logger)
    {
        _logger = logger;
    }

    public string Name => "execute_async";

    public string Description =>
        "spawns/runs/launch a command-line program in its own console window and returns immediately, " +
        "without waiting for it to exit or capturing its output. Use for interactive or " +
        "long-running programs the user will keep using and close themselves (e.g. a REPL, a " +
        "server, a TUI). For anything whose exit code or output you need to see, use 'executeSync' instead.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "command": { "type": "string", "description": "The executable to run, e.g. 'dotnet', 'notepad'." },
            "arguments": { "type": "string", "description": "Arguments to pass to the command, as a single string, e.g. 'run --project MyProject.csproj'." },
            "workingDirectory": { "type": "string", "description": "Working directory for the process. Defaults to the application's current working folder." }
          },
          "required": ["command"]
        }
        """;

    // Launches an arbitrary, unattended, long-lived process with no way to observe or
    // recall it afterwards - at least as risky as the synchronous execute tool.
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Destructive;

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, ExecuteDetachedJsonContext.Default.ExecuteDetachedArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse execute_detached arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var command = args.Command;
        var arguments = args.Arguments ?? string.Empty;
        var workingDirectory = string.IsNullOrWhiteSpace(args.WorkingDirectory) ? Directory.GetCurrentDirectory() : args.WorkingDirectory;

        if (string.IsNullOrWhiteSpace(command))
            return Task.FromResult(ToolExecutionResult.Fail("The 'command' argument is required."));

        if (!Directory.Exists(workingDirectory))
            return Task.FromResult(ToolExecutionResult.Fail($"Working directory not found: {workingDirectory}"));

        _logger.LogInformation("Launching detached: {Command} {Arguments} (cwd={WorkingDirectory})",
            command, arguments, workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            // UseShellExecute gives the child its own console window and lets it inherit
            // real keyboard/screen I/O directly from Windows, rather than from this
            // process - so it can keep running, and take input, after this tool returns.
            UseShellExecute = true,
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return Task.FromResult(ToolExecutionResult.Fail($"Failed to start \"{command}\": no process was created."));

            return Task.FromResult(ToolExecutionResult.Ok(
                $"Launched \"{command} {arguments}\" detached (PID {process.Id}) in {workingDirectory}. " +
                "It is running independently in its own window; this tool does not wait for it to exit " +
                "or capture its output."));
        }
        catch (Exception ex) when (ex is Win32Exception or SystemException)
        {
            _logger.LogError(ex, "Failed to start process {Command}", command);
            return Task.FromResult(ToolExecutionResult.Fail($"Failed to start \"{command}\": {ex.Message}"));
        }
    }
}
