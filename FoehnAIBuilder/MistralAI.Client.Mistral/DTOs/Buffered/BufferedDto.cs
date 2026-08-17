using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MistralAI.Client.DTOs.Shared;

namespace MistralAI.Client.DTOs.Buffered
{
    /// <summary>
    /// Request DTO for chat completion (buffered/non-streaming).
    /// </summary>
    public class ChatCompletionRequest
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
        /// Whether to stream back partial progress. For buffered mode, this should be false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

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
    /// A single choice from a chat completion response.
    /// </summary>
    public class ChatCompletionChoice
    {
        /// <summary>
        /// The index of this choice in the list of choices.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// The message generated by the model.
        /// </summary>
        [JsonPropertyName("message")]
        public AssistantMessage? Message { get; set; }

        /// <summary>
        /// The reason the model stopped generating tokens.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// Whether this choice is a prefix (for partial responses).
        /// </summary>
        [JsonPropertyName("prefix")]
        public bool? Prefix { get; set; }
    }

    /// <summary>
    /// Response DTO for chat completion (buffered/non-streaming).
    /// </summary>
    public class ChatCompletionResponse
    {
        /// <summary>
        /// A unique identifier for the chat completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The object type (should be "chat.completion").
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// The timestamp when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// The model that generated the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The list of completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new();

        /// <summary>
        /// Token usage information for the completion.
        /// </summary>
        [JsonPropertyName("usage")]
        public UsageInfo Usage { get; set; } = new();
    }
}
