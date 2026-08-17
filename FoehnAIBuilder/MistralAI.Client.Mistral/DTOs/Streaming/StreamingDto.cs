using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MistralAI.Client.DTOs.Shared;

namespace MistralAI.Client.DTOs.Streaming
{
    /// <summary>
    /// Request DTO for chat completion (streaming mode).
    /// </summary>
    public class ChatCompletionStreamRequest
    {
        /// <summary>
        /// ID of the model to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The prompt(s) to generate completions for, encoded as a list of messages.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<MessageBase> Messages { get; set; } = new();

        /// <summary>
        /// What sampling temperature to use. Higher values make output more random.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Nucleus sampling. Only tokens with top_p probability mass are considered.
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// The maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Stop generation if this token is detected.
        /// </summary>
        [JsonPropertyName("stop")]
        public object? Stop { get; set; } // Can be string or array of strings

        /// <summary>
        /// Whether to stream back partial progress. For streaming mode, this must be true.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        /// <summary>
        /// The seed to use for random sampling. If set, results will be deterministic.
        /// </summary>
        [JsonPropertyName("random_seed")]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Penalizes repetition of words based on their frequency.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Penalizes repetition of words or phrases.
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Specify the format that the model must output.
        /// </summary>
        [JsonPropertyName("response_format")]
        public ResponseFormat? ResponseFormat { get; set; }

        /// <summary>
        /// Whether to inject a safety prompt before all conversations.
        /// </summary>
        [JsonPropertyName("safe_prompt")]
        public bool SafePrompt { get; set; } = false;

        /// <summary>
        /// A list of tools the model may call.
        /// </summary>
        [JsonPropertyName("tools")]
        public List<Tool>? Tools { get; set; }

        /// <summary>
        /// Controls which (if any) tool is called by the model.
        /// </summary>
        [JsonPropertyName("tool_choice")]
        public object? ToolChoice { get; set; } // Can be string or ToolChoice object

        /// <summary>
        /// Whether to enable parallel function calling during tool use.
        /// </summary>
        [JsonPropertyName("parallel_tool_calls")]
        public bool ParallelToolCalls { get; set; } = true;

        /// <summary>
        /// Guardrail configurations.
        /// </summary>
        [JsonPropertyName("guardrails")]
        public List<GuardrailConfig>? Guardrails { get; set; }

        /// <summary>
        /// A cache key for prompt caching.
        /// </summary>
        [JsonPropertyName("prompt_cache_key")]
        public string? PromptCacheKey { get; set; }

        /// <summary>
        /// Available options for the prompt_mode argument.
        /// </summary>
        [JsonPropertyName("prompt_mode")]
        public string? PromptMode { get; set; }

        /// <summary>
        /// Enable users to specify an expected completion, optimizing response times.
        /// </summary>
        [JsonPropertyName("prediction")]
        public Prediction? Prediction { get; set; }

        /// <summary>
        /// Number of completions to return for each request.
        /// </summary>
        [JsonPropertyName("n")]
        public int? N { get; set; }

        /// <summary>
        /// Metadata for the request.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Reasoning effort level.
        /// </summary>
        [JsonPropertyName("reasoning_effort")]
        public string? ReasoningEffort { get; set; }
    }

    /// <summary>
    /// Base class for streaming completion events.
    /// </summary>
    public abstract class CompletionEventBase
    {
        /// <summary>
        /// The type of event.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The index of the choice this event belongs to.
        /// </summary>
        [JsonPropertyName("index")]
        public int? Index { get; set; }
    }

    /// <summary>
    /// Event containing a chunk of generated text.
    /// </summary>
    public class TextCompletionEvent : CompletionEventBase
    {
        public TextCompletionEvent() => Type = "text";

        /// <summary>
        /// The text content generated in this chunk.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// Event containing tool call information.
    /// </summary>
    public class ToolCallEvent : CompletionEventBase
    {
        public ToolCallEvent() => Type = "tool_call";

        /// <summary>
        /// The ID of the tool call.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The type of tool call.
        /// </summary>
        [JsonPropertyName("tool_type")]
        public string ToolType { get; set; } = "function";

        /// <summary>
        /// The function being called.
        /// </summary>
        [JsonPropertyName("function")]
        public FunctionCall? Function { get; set; }
    }

    /// <summary>
    /// Event containing tool call delta (streaming chunk).
    /// </summary>
    public class ToolCallDeltaEvent : CompletionEventBase
    {
        public ToolCallDeltaEvent() => Type = "tool_call_delta";

        /// <summary>
        /// The ID delta of the tool call.
        /// </summary>
        [JsonPropertyName("id")]
        public string? IdDelta { get; set; }

        /// <summary>
        /// The type of tool call.
        /// </summary>
        [JsonPropertyName("tool_type")]
        public string ToolType { get; set; } = "function";

        /// <summary>
        /// The function call delta.
        /// </summary>
        [JsonPropertyName("function")]
        public FunctionCallDelta? FunctionDelta { get; set; }
    }

    /// <summary>
    /// Delta information for a function call during streaming.
    /// </summary>
    public class FunctionCallDelta
    {
        /// <summary>
        /// Delta for the function name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? NameDelta { get; set; }

        /// <summary>
        /// Delta for the function arguments.
        /// </summary>
        [JsonPropertyName("arguments")]
        public string? ArgumentsDelta { get; set; }
    }

    /// <summary>
    /// Event containing tool message information.
    /// </summary>
    public class ToolMessageEvent : CompletionEventBase
    {
        public ToolMessageEvent() => Type = "tool_message";

        /// <summary>
        /// The role of the message.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "tool";

        /// <summary>
        /// The content of the tool message.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// The tool call ID this message is responding to.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// Event containing tool message delta (streaming chunk).
    /// </summary>
    public class ToolMessageDeltaEvent : CompletionEventBase
    {
        public ToolMessageDeltaEvent() => Type = "tool_message_delta";

        /// <summary>
        /// Delta for the role.
        /// </summary>
        [JsonPropertyName("role")]
        public string? RoleDelta { get; set; }

        /// <summary>
        /// Delta for the content.
        /// </summary>
        [JsonPropertyName("content")]
        public string? ContentDelta { get; set; }

        /// <summary>
        /// The tool call ID this message is responding to.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// Event indicating the completion is done.
    /// </summary>
    public class DoneCompletionEvent : CompletionEventBase
    {
        public DoneCompletionEvent() => Type = "done";

        /// <summary>
        /// The reason the completion finished.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// Token usage information for the completion.
        /// </summary>
        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }
    }

    /// <summary>
    /// Event indicating an error occurred.
    /// </summary>
    public class ErrorCompletionEvent : CompletionEventBase
    {
        public ErrorCompletionEvent() => Type = "error";

        /// <summary>
        /// The error message.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// The wrapper for completion events in the streaming response.
    /// </summary>
    public class CompletionEvent
    {
        /// <summary>
        /// The type of event.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// The data associated with the event.
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }

        /// <summary>
        /// The index of the choice this event belongs to.
        /// </summary>
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        /// <summary>
        /// For text events, the text content.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// For done events, the finish reason.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// For done events, the usage information.
        /// </summary>
        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }

        /// <summary>
        /// For tool call events, the tool call ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// For tool call events, the function call information.
        /// </summary>
        [JsonPropertyName("function")]
        public object? Function { get; set; }

        /// <summary>
        /// For error events, the error message.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Enumeration of streaming event types.
    /// </summary>
    public enum CompletionEventType
    {
        Text,
        ToolCall,
        ToolCallDelta,
        ToolMessage,
        ToolMessageDelta,
        Done,
        Error
    }

    /// <summary>
    /// Enumeration of finish reasons.
    /// </summary>
    public enum FinishReason
    {
        Stop,
        Length,
        Error,
        Cancelled,
        ToolCalls
    }
}
