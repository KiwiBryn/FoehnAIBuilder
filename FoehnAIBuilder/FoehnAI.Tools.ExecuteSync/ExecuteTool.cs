using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FoehnAIBuilder.Abstractions;
using FoehnSharp.Tools.ExecuteSync;
using Microsoft.Extensions.Logging;

namespace FoehnAIBuilder.Tools.ExecuteSync;

/// <summary>
/// Runs a command-line program and returns its exit code, standard output, and
/// standard error. Commands run with no confirmation prompt or allowlist - the LLM is
/// trusted the same way it is in Claude Code - but every run is bounded by a timeout.
/// </summary>
public sealed class ExecuteTool : ITool
{
    private const int DefaultTimeoutSeconds = 120;
    private const int MaxOutputCharacters = 100_000;

    private readonly ILogger<ExecuteTool> _logger;

    public ExecuteTool(ILogger<ExecuteTool> logger)
    {
        _logger = logger;
    }

    public string Name => "execute_sync";

    public string Description =>
        "Runs a command-line program (e.g. dotnet, git, npm) and returns its exit code, standard " +
        "output, and standard error. Use for builds, tests, version control, or any other shell command" +
        "prefer execute_async";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "command": { "type": "string", "description": "The executable to run, e.g. 'dotnet', 'git', 'cmd'." },
            "arguments": { "type": "string", "description": "Arguments to pass to the command, as a single string, e.g. 'build MyProject.csproj'." },
            "workingDirectory": { "type": "string", "description": "Working directory for the process. Defaults to the application's current working folder." },
            "timeoutSeconds": { "type": "integer", "description": "Maximum seconds to wait before the process is killed. Defaults to 120." }
          },
          "required": ["command"]
        }
        """;

    // The command is arbitrary and its effects aren't known ahead of time, so treat it
    // as the most cautious tier - the same as delete/rmdir.
    public ToolRiskLevel RiskLevel => ToolRiskLevel.Destructive;

    public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, ExecuteJsonContext.Default.ExecuteArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse execute arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return ToolExecutionResult.Fail(jsonError!);
        }

        var command = args.Command;
        var arguments = args.Arguments ?? string.Empty;
        var workingDirectory = string.IsNullOrWhiteSpace(args.WorkingDirectory) ? Directory.GetCurrentDirectory() : args.WorkingDirectory;
        var timeoutSeconds = args.TimeoutSeconds ?? DefaultTimeoutSeconds;

        if (string.IsNullOrWhiteSpace(command))
            return ToolExecutionResult.Fail("The 'command' argument is required.");

        if (!Directory.Exists(workingDirectory))
            return ToolExecutionResult.Fail($"Working directory not found: {workingDirectory}");

        _logger.LogInformation("Executing: {Command} {Arguments} (cwd={WorkingDirectory}, timeout={TimeoutSeconds}s)",
            command, arguments, workingDirectory, timeoutSeconds);

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        try
        {
            process.Start();

            // Without this, the child inherits FoehnSharpV1's own live console handle
            // (since RedirectStandardInput alone doesn't disconnect it until closed), so
            // anything the child reads from stdin blocks forever - nobody is typing into
            // it on the child's behalf. Closing it immediately gives every spawned
            // process instant EOF, matching how a non-interactive/automated run behaves.
            process.StandardInput.Close();
        }
        catch (Exception ex) when (ex is Win32Exception or SystemException)
        {
            _logger.LogError(ex, "Failed to start process {Command}", command);
            return ToolExecutionResult.Fail($"Failed to start \"{command}\": {ex.Message}");
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
        }

        var stdOut = await SafeAwait(stdOutTask);
        var stdErr = await SafeAwait(stdErrTask);

        var report = new StringBuilder();
        report.AppendLine(timedOut
            ? $"Command timed out after {timeoutSeconds}s and was killed."
            : $"Exit code: {process.ExitCode}");
        if (!string.IsNullOrEmpty(stdOut))
            report.AppendLine("--- stdout ---").AppendLine(Truncate(stdOut));
        if (!string.IsNullOrEmpty(stdErr))
            report.AppendLine("--- stderr ---").AppendLine(Truncate(stdErr));

        bool success = !timedOut && process.ExitCode == 0;
        return success ? ToolExecutionResult.Ok(report.ToString()) : ToolExecutionResult.Fail(report.ToString());
    }

    private static string Truncate(string text) =>
        text.Length > MaxOutputCharacters
            ? text[..MaxOutputCharacters] + $"\n[Output truncated at {MaxOutputCharacters} characters.]"
            : text;

    private static async Task<string> SafeAwait(Task<string> task)
    {
        try
        {
            return await task;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill timed-out process");
        }
    }
}
