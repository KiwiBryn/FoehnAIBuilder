using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralAI.Client.DTOs.Shared
{
   /// <summary>
   /// Represents a message in a chat conversation.
   /// </summary>
   [JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
   [JsonDerivedType(typeof(SystemMessage), "system")]
   [JsonDerivedType(typeof(UserMessage), "user")]
   [JsonDerivedType(typeof(AssistantMessage), "assistant")]
   [JsonDerivedType(typeof(ToolMessage), "tool")]
   public abstract class MessageBase
    {
        /// <summary>
        /// The role of the message author.
        /// </summary>
       [JsonIgnore]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    /// <summary>
    /// A message from the system.
    /// </summary>
    public class SystemMessage : MessageBase
    {
        public SystemMessage() => Role = "system";
    }

    /// <summary>
    /// A message from the user.
    /// </summary>
    public class UserMessage : MessageBase
    {
        public UserMessage() => Role = "user";
    }

    /// <summary>
    /// A message from the assistant.
    /// </summary>
    public class AssistantMessage : MessageBase
    {
        public AssistantMessage() => Role = "assistant";

        /// <summary>
        /// Tool calls made by the assistant.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }

    /// <summary>
    /// A message from a tool.
    /// </summary>
    public class ToolMessage : MessageBase
    {
        public ToolMessage() => Role = "tool";

        /// <summary>
        /// The tool call ID that this message is responding to.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// Represents a tool call made by the model.
    /// </summary>
    public class ToolCall
    {
        /// <summary>
        /// The ID of the tool call.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of tool call.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// The function being called.
        /// </summary>
        [JsonPropertyName("function")]
        public FunctionCall? Function { get; set; }
    }

    /// <summary>
    /// Represents a function call.
    /// </summary>
    public class FunctionCall
    {
        /// <summary>
        /// The name of the function.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The arguments for the function call.
        /// </summary>
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a tool available for the model to use.
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// The type of tool.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// The function definition.
        /// </summary>
        [JsonPropertyName("function")]
        public FunctionDefinition? Function { get; set; }
    }

    /// <summary>
    /// Represents a function definition for a tool.
    /// </summary>
    public class FunctionDefinition
    {
        /// <summary>
        /// The name of the function.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The description of the function.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The parameters of the function.
        /// </summary>
        [JsonPropertyName("parameters")]
        public object? Parameters { get; set; }

        /// <summary>
        /// Whether the function is strict about parameter types.
        /// </summary>
        [JsonPropertyName("strict")]
        public bool? Strict { get; set; }
    }

    /// <summary>
    /// Response format configuration.
    /// </summary>
    public class ResponseFormat
    {
        /// <summary>
        /// The type of response format.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        /// <summary>
        /// JSON schema for the response (used when type is "json_schema").
        /// </summary>
        [JsonPropertyName("schema")]
        public object? Schema { get; set; }
    }

    /// <summary>
    /// Configuration for tool choice behavior.
    /// </summary>
    public class ToolChoice
    {
        /// <summary>
        /// The type of tool choice.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The function to call (used when type is "function").
        /// </summary>
        [JsonPropertyName("function")]
        public FunctionCall? Function { get; set; }
    }

    /// <summary>
    /// Prediction configuration for optimizing response times.
    /// </summary>
    public class Prediction
    {
        /// <summary>
        /// The expected completion text.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// The tokens to predict.
        /// </summary>
        [JsonPropertyName("tokens")]
        public List<int>? Tokens { get; set; }
    }

    /// <summary>
    /// Guardrail configuration.
    /// </summary>
    public class GuardrailConfig
    {
        // Guardrail configuration properties would go here
        // Simplified for initial implementation
    }

    /// <summary>
    /// Token usage information.
    /// </summary>
    public class UsageInfo
    {
        /// <summary>
        /// Number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens (prompt + completion).
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }

        /// <summary>
        /// Number of cached tokens.
        /// </summary>
        [JsonPropertyName("num_cached_tokens")]
        public int? NumCachedTokens { get; set; }

        /// <summary>
        /// Number of audio seconds in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_audio_seconds")]
        public double? PromptAudioSeconds { get; set; }

        /// <summary>
        /// Detailed prompt token information.
        /// </summary>
        [JsonPropertyName("prompt_tokens_details")]
        public object? PromptTokensDetails { get; set; }

        /// <summary>
        /// Detailed completion token information.
        /// </summary>
        [JsonPropertyName("completion_tokens_details")]
        public object? CompletionTokensDetails { get; set; }
    }

    /// <summary>
    /// Enumeration of message roles.
    /// </summary>
    public enum MessageRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    /// <summary>
    /// Enumeration of reasoning effort levels.
    /// </summary>
    public enum ReasoningEffort
    {
        None,
        Minimal,
        Low,
        Medium,
        High,
        XHigh
    }

    /// <summary>
    /// Enumeration of tool choice options.
    /// </summary>
    public enum ToolChoiceOption
    {
        Auto,
        None,
        Any,
        Required
    }
}
