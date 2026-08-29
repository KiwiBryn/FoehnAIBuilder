// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Configuration;

namespace FoehnAIBuilder.Chat;

/// <summary>
/// Reads the system prompt from the path in <see cref="FoehnAIBuilderOptions.SystemMessageFile"/>. 
/// Skills are loaded on-demand when requested via /skillname commands.
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
      _cached = new Lazy<string?>(LoadBasePrompt);
   }

   public string? SystemPrompt() => _cached.Value;

   public string? SystemPromptFilename() => _options.SystemMessageFile;

   /// <summary>
   /// Loads a specific skill by name from the .skills folder.
   /// </summary>
   /// <param name="skillName">The name of the skill (without .md extension)</param>
   /// <returns>The skill content if found, null otherwise</returns>
   public string? LoadSkill(string skillName)
   {
      //var skillsPath = Path.Combine(AppContext.BaseDirectory, ".skills");
      var skillsPath = _options.SkillsPath;

      if (!Directory.Exists(skillsPath))
      {
         _logger.LogDebug("Skills directory '{Path}' not found.", skillsPath);
         return null;
      }

      try
      {
         var skillFile = Path.Combine(skillsPath, $"{skillName}.md");
         
         if (!File.Exists(skillFile))
         {
            _logger.LogDebug("Skill file '{Path}' not found.", skillFile);
            return null;
         }

         var skillContent = File.ReadAllText(skillFile);
         
         if (string.IsNullOrWhiteSpace(skillContent))
         {
            _logger.LogDebug("Skill file '{Path}' is empty.", skillFile);
            return null;
         }

         return skillContent.Trim();
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to load skill '{SkillName}'.", skillName);
         return null;
      }
   }

   /// <summary>
   /// Gets a list of available skill names (without .md extension)
   /// </summary>
   /// <returns>List of available skill names</returns>
   public List<string> GetAvailableSkills()
   {
      //var skillsPath = Path.Combine(AppContext.BaseDirectory, ".skills");
      var skillsPath = _options.SkillsPath;
      var skills = new List<string>();
      
      if (!Directory.Exists(skillsPath))
      {
         return skills;
      }

      try
      {
         var skillFiles = Directory.GetFiles(skillsPath, "*.md");
         
         foreach (var skillFile in skillFiles)
         {
            var skillName = Path.GetFileNameWithoutExtension(skillFile);
            skills.Add(skillName);
         }
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to enumerate skills from '{Path}'.", skillsPath);
      }

      return skills;
   }

   private string? LoadBasePrompt()
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