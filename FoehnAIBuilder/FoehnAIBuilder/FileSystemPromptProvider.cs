// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Configuration;

namespace FoehnAIBuilder.Chat;

/// <summary>
/// Reads the system prompt from the path in <see cref="FoehnAIBuilderOptions.SystemMessageFile"/>.
/// Missing / empty / unreadable files log a warning and produce no prompt.
/// </summary>
public sealed class FileSystemPromptProvider : ISystemPromptProvider
{
   private readonly FoehnAIBuilderOptions _options;
   private readonly ILogger<FileSystemPromptProvider> _logger;
   private readonly Lazy<string?> _cached;

   public FileSystemPromptProvider(IOptions<FoehnAIBuilderOptions> options, ILogger<FileSystemPromptProvider> logger)
   {
      _options = options.Value;
      _logger = logger;
      _cached = new Lazy<string?>(Load);
   }

   public string? SystemPrompt() => _cached.Value;

   public string? SystemPromptFilename() => _options.SystemMessageFile;


   private string? Load()
   {
      var path = Path.IsPathRooted(_options.SystemMessageFile)
          ? _options.SystemMessageFile
          : Path.Combine(AppContext.BaseDirectory, _options.SystemMessageFile);

      if (!File.Exists(path))
      {
         _logger.LogWarning("System message file '{Path}' was not found; continuing without a system prompt.", path);
         return null;
      }

      try
      {
         var content = File.ReadAllText(path);
         if (string.IsNullOrWhiteSpace(content))
         {
            _logger.LogWarning("System message file '{Path}' is empty; continuing without a system prompt.", path);
            return null;
         }
         return content;
      }
      catch (IOException ex)
      {
         _logger.LogError(ex, "Failed to read system message file '{Path}'.", path);
         return null;
      }
   }
}