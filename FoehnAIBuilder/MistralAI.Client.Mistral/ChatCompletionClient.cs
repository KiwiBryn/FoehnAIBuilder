using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using MistralAI.Client.DTOs.Buffered;
using MistralAI.Client.DTOs.Shared;
using MistralAI.Client.DTOs.Streaming;

namespace MistralAI.Client
{
    /// <summary>
    /// Configuration options for the Mistral AI chat completion client.
    /// </summary>
    public class MistralAiOptions
    {
        /// <summary>
        /// The base URL for the Mistral AI API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.mistral.ai";

        /// <summary>
        /// The API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The default model to use for completions.
        /// </summary>
        public string DefaultModel { get; set; } = "mistral-large-latest";

        /// <summary>
        /// Timeout for HTTP requests in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Whether to enable streaming by default.
        /// </summary>
        public bool EnableStreaming { get; set; } = false;
    }

    /// <summary>
    /// Exception thrown when the Mistral AI API returns an error.
    /// </summary>
    public class MistralAiException : Exception
    {
        /// <summary>
        /// The HTTP status code returned by the API.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The error details from the API response.
        /// </summary>
        public string? ErrorDetails { get; }

        public MistralAiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string? errorDetails = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorDetails = errorDetails;
        }
    }

    /// <summary>
    /// Client for interacting with the Mistral AI chat completion API.
    /// Uses HttpClientFactory for dependency injection and Polly for resilience.
    /// </summary>
    public class ChatCompletionClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly MistralAiOptions _options;
        private readonly ILogger<ChatCompletionClient>? _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ChatCompletionClient.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="options">The Mistral AI options.</param>
        /// <param name="logger">Optional logger.</param>
        public ChatCompletionClient(
            IHttpClientFactory httpClientFactory,
            IOptions<MistralAiOptions> options,
            ILogger<ChatCompletionClient>? logger = null)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value ?? new MistralAiOptions();
            _logger = logger;

            // Configure HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MistralAI.Client/1.0");

            // Configure retry policy
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => !msg.IsSuccessStatusCode && 
                    (msg.StatusCode == HttpStatusCode.TooManyRequests ||
                     msg.StatusCode == HttpStatusCode.RequestTimeout ||
                     msg.StatusCode == HttpStatusCode.InternalServerError ||
                     msg.StatusCode == HttpStatusCode.BadGateway ||
                     msg.StatusCode == HttpStatusCode.ServiceUnavailable ||
                     msg.StatusCode == HttpStatusCode.GatewayTimeout))
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        _logger?.LogWarning(
                            "Mistral AI API request failed with status {StatusCode}. Retrying in {Delay}s (attempt {RetryCount}/{MaxRetries})",
                            response.Result?.StatusCode ?? HttpStatusCode.InternalServerError,
                            delay.TotalSeconds,
                            retryCount,
                            _options.MaxRetries);
                    });
        }

        /// <summary>
        /// Initializes a new instance of the ChatCompletionClient with a custom HttpClient.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="baseUrl">The base URL for the API.</param>
        /// <param name="defaultModel">The default model to use.</param>
        /// <param name="logger">Optional logger.</param>
        public ChatCompletionClient(
            HttpClient httpClient,
            string apiKey,
            string baseUrl = "https://api.mistral.ai",
            string defaultModel = "mistral-large-latest",
            ILogger<ChatCompletionClient>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = new MistralAiOptions
            {
                BaseUrl = baseUrl,
                ApiKey = apiKey,
                DefaultModel = defaultModel
            };
            _logger = logger;

            // Configure HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MistralAI.Client/1.0");

            // Configure retry policy
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => !msg.IsSuccessStatusCode && 
                    (msg.StatusCode == HttpStatusCode.TooManyRequests ||
                     msg.StatusCode == HttpStatusCode.RequestTimeout ||
                     msg.StatusCode == HttpStatusCode.InternalServerError ||
                     msg.StatusCode == HttpStatusCode.BadGateway ||
                     msg.StatusCode == HttpStatusCode.ServiceUnavailable ||
                     msg.StatusCode == HttpStatusCode.GatewayTimeout))
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        _logger?.LogWarning(
                            "Mistral AI API request failed with status {StatusCode}. Retrying in {Delay}s (attempt {RetryCount}/{MaxRetries})",
                            response.Result?.StatusCode ?? HttpStatusCode.InternalServerError,
                            delay.TotalSeconds,
                            retryCount,
                            _options.MaxRetries);
                    });
        }

        /// <summary>
        /// Creates a chat completion request with the specified parameters.
        /// </summary>
        /// <param name="model">The model to use for completion.</param>
        /// <param name="messages">The list of messages to send.</param>
        /// <param name="temperature">The sampling temperature.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate.</param>
        /// <param name="stream">Whether to stream the response.</param>
        /// <returns>A configured ChatCompletionRequest.</returns>
        public ChatCompletionRequest CreateRequest(
            string? model = null,
            List<MessageBase>? messages = null,
            double? temperature = null,
            int? maxTokens = null,
            bool stream = false)
        {
            return new ChatCompletionRequest
            {
                Model = model ?? _options.DefaultModel,
                Messages = messages ?? new List<MessageBase>(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = stream
            };
        }

        /// <summary>
        /// Creates a chat completion request with the specified parameters.
        /// </summary>
        /// <param name="model">The model to use for completion.</param>
        /// <param name="messages">The list of messages to send.</param>
        /// <param name="temperature">The sampling temperature.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate.</param>
        /// <returns>A configured ChatCompletionRequest.</returns>
        public ChatCompletionStreamRequest CreateStreamRequest(
            string? model = null,
            List<MessageBase>? messages = null,
            double? temperature = null,
            int? maxTokens = null)
        {
            return new ChatCompletionStreamRequest
            {
                Model = model ?? _options.DefaultModel,
                Messages = messages ?? new List<MessageBase>(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = true
            };
        }

        /// <summary>
        /// Gets a chat completion in buffered (non-streaming) mode asynchronously.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The chat completion response.</returns>
        /// <exception cref="MistralAiException">Thrown when the API request fails.</exception>
        public async Task<ChatCompletionResponse> GetCompletionAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Model))
                request.Model = _options.DefaultModel;

            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("At least one message is required.", nameof(request));

            // Ensure stream is false for buffered mode
            request.Stream = false;

            var requestUri = $"{_options.BaseUrl}/v1/chat/completions";
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger?.LogDebug("Sending chat completion request to {RequestUri}", requestUri);

            try
            {
                var response = await _retryPolicy.ExecuteAsync(async ct =>
                    await _httpClient.PostAsync(requestUri, requestContent, ct), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError(
                        "Mistral AI API request failed with status {StatusCode}. Response: {ErrorContent}",
                        response.StatusCode,
                        errorContent);

                    throw new MistralAiException(
                        "Mistral AI API request failed",
                        response.StatusCode,
                        errorContent);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger?.LogDebug("Received chat completion response: {ResponseJson}", responseJson);

                var responseData = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, GetJsonOptions());
                if (responseData == null)
                    throw new MistralAiException("Failed to deserialize response from Mistral AI API.", HttpStatusCode.InternalServerError);

                return responseData;
            }
            catch (Exception ex) when (ex is not MistralAiException)
            {
                _logger?.LogError(ex, "Error calling Mistral AI chat completion API");
                throw new MistralAiException("Error calling Mistral AI chat completion API", HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Gets a chat completion in streaming mode asynchronously.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>An async enumerable of completion events.</returns>
        /// <exception cref="MistralAiException">Thrown when the API request fails.</exception>
        public async IAsyncEnumerable<CompletionEvent> StreamCompletionAsync(
            ChatCompletionStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Model))
                request.Model = _options.DefaultModel;

            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("At least one message is required.", nameof(request));

            // Ensure stream is true for streaming mode
            request.Stream = true;

            var requestUri = $"{_options.BaseUrl}/v1/chat/completions";
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger?.LogDebug("Sending streaming chat completion request to {RequestUri}", requestUri);

            HttpResponseMessage? response = null;

            try
            {
                response = await _retryPolicy.ExecuteAsync(async ct =>
                    await _httpClient.PostAsync(requestUri, requestContent, ct), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError(
                        "Mistral AI API streaming request failed with status {StatusCode}. Response: {ErrorContent}",
                        response.StatusCode,
                        errorContent);

                    throw new MistralAiException(
                        "Mistral AI API streaming request failed",
                        response.StatusCode,
                        errorContent);
                }

                var responseStream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(responseStream);

                string? line;
                while ((line = await streamReader.ReadLineAsync(cancellationToken)) != null)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Handle SSE format (data: ...)
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6).Trim();
                        if (data == "[DONE]")
                        {
                            _logger?.LogDebug("Received [DONE] event");
                            break;
                        }

                        CompletionEvent? @event = null;
                        try
                        {
                            @event = JsonSerializer.Deserialize<CompletionEvent>(data, GetJsonOptions());
                            if (@event != null)
                            {
                                _logger?.LogDebug("Received streaming event: {EventType}", @event.Type);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to deserialize streaming event: {Line}", line);
                            // Try to create a basic event from the line
                            @event = new CompletionEvent { Type = "unknown", Text = line };
                        }

                        if (@event != null)
                            yield return @event;
                    }
                }
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Gets a chat completion in buffered mode with a simple message.
        /// </summary>
        /// <param name="message">The user message.</param>
        /// <param name="model">The model to use.</param>
        /// <param name="systemMessage">Optional system message.</param>
        /// <param name="temperature">Optional temperature setting.</param>
        /// <param name="maxTokens">Optional max tokens.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The chat completion response.</returns>
        public async Task<ChatCompletionResponse> GetCompletionAsync(
            string message,
            string? model = null,
            string? systemMessage = null,
            double? temperature = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<MessageBase>();

            if (!string.IsNullOrWhiteSpace(systemMessage))
                messages.Add(new SystemMessage { Content = systemMessage });

            messages.Add(new UserMessage { Content = message });

            var request = CreateRequest(model, messages, temperature, maxTokens, false);
            return await GetCompletionAsync(request, cancellationToken);
        }

        /// <summary>
        /// Streams a chat completion with a simple message.
        /// </summary>
        /// <param name="message">The user message.</param>
        /// <param name="model">The model to use.</param>
        /// <param name="systemMessage">Optional system message.</param>
        /// <param name="temperature">Optional temperature setting.</param>
        /// <param name="maxTokens">Optional max tokens.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>An async enumerable of completion events.</returns>
        public async IAsyncEnumerable<string> StreamCompletionTextAsync(
            string message,
            string? model = null,
            string? systemMessage = null,
            double? temperature = null,
            int? maxTokens = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messages = new List<MessageBase>();

            if (!string.IsNullOrWhiteSpace(systemMessage))
                messages.Add(new SystemMessage { Content = systemMessage });

            messages.Add(new UserMessage { Content = message });

            var request = CreateStreamRequest(model, messages, temperature, maxTokens);

            await foreach (var @event in StreamCompletionAsync(request, cancellationToken))
            {
                if (@event.Type == "text" && !string.IsNullOrEmpty(@event.Text))
                    yield return @event.Text;
            }
        }

        /// <summary>
        /// Gets the JSON serialization options with the correct configuration.
        /// </summary>
        /// <returns>JsonSerializerOptions configured for the Mistral AI API.</returns>
        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Disposes the client and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">Whether we are disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up.
        /// </summary>
        ~ChatCompletionClient()
        {
            Dispose(false);
        }
    }
}
