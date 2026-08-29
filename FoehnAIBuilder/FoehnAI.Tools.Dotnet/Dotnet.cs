// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;
using FoehnAI.Tools.ExecuteSync;


namespace FoehnAIBuilder.Tools.Dotnet;

/// <summary>
/// Runs the dotnet  command-line program and returns its exit code, standard output, and
/// standard error. Dotnet runs with no a "write" prompt - the LLM is sort of trusted, 
//  but every run is bounded by a timeout. This is a more specific version of ExecuteTool
//  that is limited to dotnet commands.
/// </summary>
public sealed class DotnetTool : ITool
{
   private const int DefaultTimeoutSeconds = 120;
   private const int MaxOutputCharacters = 100_000;

   private readonly ILogger<DotnetTool> _logger;

   public DotnetTool(ILogger<DotnetTool> logger)
   {
      _logger = logger;
   }

   public string Name => "dotnet";
   private static readonly HashSet<string> AllowedCommands = new()
    {
        "new",
        "build",
        "run",
        "test",
        "restore",
        "clean",
        "--list-sdks",
        "--list-runtimes",
        "--info",
        "--help" 
   };

   public string Description =>
       $@"Runs a restricted dotnet CLI subcommand( {string.Join(",", AllowedCommands)})" +
       "and returns its exit code, standard output, and standard error." +
       "When creating new projects, the LLM is trusted to choose the project type and name, but every run is bounded by a timeout." +
       "solutions and projects are created a subdirectory of the same name as the project, and the LLM is trusted to choose the project type and name." +
       "For anything outside this subcommand list, use invoke.sync instead.";

   public string Command => """
        {
          "type": "object",
          "properties": {
            "arguments": { "type": "string", "description": "The dotnet subcommand and its arguments, as a single string, e.g. 'build MyProject.csproj' or 'test --filter Category=Fast'. The first word must be one of: new, build, run, test, restore, clean, --list-sdks, --list-runtimes, --info, --help." },
            "workingDirectory": { "type": "string", "description": "Working directory for the process. Defaults to the application's current working folder." },
            "timeoutSeconds": { "type": "integer", "description": "Maximum seconds to wait before the process is killed. Defaults to 120." }
          },
          "required": ["arguments"]
        }
        """;

   // The dotnet tool is "scoped" so for now not treated as a "destructive"
   public ToolRiskLevel RiskLevel => ToolRiskLevel.Write;

   public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
   {
      if (!ToolArguments.TryParse(argumentsJson, DotnetJsonContext.Default.DotnetArguments, out var args, out var jsonError))
      {
         _logger.LogWarning("Failed to parse dotnet arguments: {Arguments} ({Error})", argumentsJson, jsonError);
         return ToolExecutionResult.Fail(jsonError!);
      }

      var arguments = args.Arguments;
      var workingDirectory = string.IsNullOrWhiteSpace(args.WorkingDirectory) ? Directory.GetCurrentDirectory() : args.WorkingDirectory;
      var timeoutSeconds = args.TimeoutSeconds ?? DefaultTimeoutSeconds;

      if (string.IsNullOrWhiteSpace(arguments))
         return ToolExecutionResult.Fail("The 'arguments' argument is required.");

      var sandboxRoot = Directory.GetCurrentDirectory();
      if (!ToolPath.TryResolve(sandboxRoot, workingDirectory, out var fullWorkingDirectory, out var pathError))
         return ToolExecutionResult.Fail(pathError!);

      if (!Directory.Exists(fullWorkingDirectory))
         return ToolExecutionResult.Fail($"Working directory not found: {workingDirectory}");

      _logger.LogInformation("Executing: dotnet {Arguments} (cwd={WorkingDirectory}, timeout={TimeoutSeconds}s)",
          arguments, workingDirectory, timeoutSeconds);

      var subcommand = arguments.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0];
      if (!AllowedCommands.Contains(subcommand))
      {
         return ToolExecutionResult.Fail(
             $"Subcommand \"{subcommand}\" is not allowed. Allowed subcommands: {string.Join(", ", AllowedCommands)}.");
      }

      var startInfo = new ProcessStartInfo
      {
         FileName = "dotnet",
         Arguments = arguments,
         WorkingDirectory = fullWorkingDirectory,
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

         // Without this, the child inherits FoehnAI's own live console handle
         // (since RedirectStandardInput alone doesn't disconnect it until closed), so
         // anything the child reads from stdin blocks forever - nobody is typing into
         // it on the child's behalf. Closing it immediately gives every spawned
         // process instant EOF, matching how a non-interactive/automated run behaves.
         process.StandardInput.Close();
      }
      catch (Exception ex) when (ex is Win32Exception or SystemException)
      {
         _logger.LogError(ex, "Failed to start process dotnet");
         return ToolExecutionResult.Fail($"Failed to start \"dotnet\": {ex.Message}");
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
