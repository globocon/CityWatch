using CityWatch.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// The model half of AI Assistance: rewriting an incident report, and converting it to another
    /// language or regional English variant.
    /// </summary>
    public interface IAiService
    {
        /// <summary>False when the platform is switched off or no key is configured; the feature then reports itself unavailable.</summary>
        bool IsConfigured { get; }

        Task<string> ImproveIncidentReportAsync(string text, CancellationToken cancellationToken = default);

        Task<string> TranslateAsync(string text, string targetLanguageName, CancellationToken cancellationToken = default);
    }

    /// <summary>Raised when the model provider cannot be reached or answers with something unusable.</summary>
    public class AiServiceUnavailableException : Exception
    {
        public AiServiceUnavailableException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Claude, configured from the "Ai" section - the same provider, section name and settings shape
    /// the SmartRosterAI platform uses, so a deployment configures both the same way.
    ///
    /// Deliberately the Anthropic Messages REST endpoint over a typed HttpClient rather than the
    /// official Anthropic .NET SDK that SmartRoster uses. The SDK targets net8.0+ and drags
    /// System.Text.Json 10.x, System.IO.Pipelines and System.Net.ServerSentEvents into the app;
    /// CityWatch.Web is net7.0, and those packages state they do not support it. Taking them would
    /// swap the JSON serialiser for the whole application - every existing JsonResult on every page -
    /// to satisfy one text-rewrite feature, and that class of breakage only shows up at runtime.
    /// Switch to the SDK once CityWatch.Web moves to net8.0 or later; nothing outside this file
    /// would change.
    ///
    /// Single-shot and non-streaming: this is one text transform per click, not an agent turn, so
    /// none of SmartRoster's tool-loop or streaming machinery applies.
    /// </summary>
    public class AiService : IAiService
    {
        /// <summary>Pinned wire version, as required on every Anthropic API request.</summary>
        private const string AnthropicVersion = "2023-06-01";

        private readonly HttpClient _http;
        private readonly AiOptions _options;
        private readonly ILogger<AiService> _logger;

        /* The rewrite instruction, sent as the system prompt. It previously existed as a local
           variable that was never referenced - the request carried the guard's raw text with no
           instructions at all, which is why the output was unpredictable. */
        internal const string ImproveInstructions = @"
You are a writing assistant for security incident reports. You are not an investigator.

Rewrite the supplied incident report into a clear, professional and detailed security incident report.

- Correct grammar, spelling and sentence structure.
- Improve clarity and readability.
- Expand short descriptions only by rephrasing what is already stated.
- Preserve every factual detail from the original text.
- Preserve the original formatting: keep the existing line breaks, blank lines, paragraph breaks,
  bullet points and numbering. Do not collapse a multi-line report into one paragraph and do not
  introduce bullets where the original has none.

You must NOT invent people, dates, times, locations, actions, conversations, motives, events,
outcomes, evidence, or any information that is not present in the source text. If information is
missing, leave it missing - do not guess it and do not insert placeholders.

Return only the improved incident report, with no preamble, commentary or questions.";

        /* Deliberately separate from the rewrite instruction: a translation must not also 'improve'
           the text, or the guard gets two changes when they asked for one. */
        internal const string TranslateInstructionsFormat = @"
Translate or convert the supplied text from its automatically detected source language into
{0}.

- Preserve the original meaning and all factual information.
- Do not add information.
- Do not remove information.
- Do not invent facts.
- Preserve the original formatting: keep the existing line breaks, blank lines, paragraph breaks,
  bullet points and numbering exactly as they appear in the source.
- For a regional English variant, keep the wording and adapt spelling, terminology and phrasing to
  that variant (for Australian English prefer forms such as organisation, authorised, centre).

Return only the converted text, with no preamble or commentary.";

        public AiService(HttpClient http, IOptions<AiOptions> options, ILogger<AiService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

        public Task<string> ImproveIncidentReportAsync(string text, CancellationToken cancellationToken = default) =>
            CompleteAsync(ImproveInstructions, text, cancellationToken);

        public Task<string> TranslateAsync(string text, string targetLanguageName, CancellationToken cancellationToken = default) =>
            CompleteAsync(string.Format(TranslateInstructionsFormat, targetLanguageName), text, cancellationToken);

        private async Task<string> CompleteAsync(string systemPrompt, string input, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                /* The old OpenAI path called the provider with a null key, turned the 401 into the
                   literal string "OpenAI error" and let the page write that over the guard's report.
                   Fail before any request instead. */
                _logger.LogError("AI operation failed: the AI platform is disabled or no key is configured (Ai:ApiKey).");
                throw new AiServiceUnavailableException("No AI provider key is configured.");
            }

            var body = JsonSerializer.Serialize(new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokensPerTurn,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = input } }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            /* Per request, not on DefaultRequestHeaders: the typed client is shared across concurrent
               requests and mutating its default headers is not thread-safe. */
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", AnthropicVersion);

            string json;
            try
            {
                var response = await _http.SendAsync(request, cancellationToken);
                json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // Provider detail goes to the log; the guard sees a generic message.
                    _logger.LogError("AI operation failed: provider returned {StatusCode}. Body: {Body}",
                        (int)response.StatusCode, Truncate(json, 500));
                    throw new AiServiceUnavailableException($"The AI provider returned {(int)response.StatusCode}.");
                }
            }
            catch (AiServiceUnavailableException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("AI operation failed: the provider timed out.");
                throw new AiServiceUnavailableException("The AI provider timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI operation failed: the provider could not be reached.");
                throw new AiServiceUnavailableException("The AI provider could not be reached.", ex);
            }

            var text = ExtractText(json);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("AI operation failed: no text content in the provider response. Body: {Body}",
                    Truncate(json, 500));
                throw new AiServiceUnavailableException("The AI provider returned no usable text.");
            }

            /* Trimmed at the ends only. Trimming each line, or normalising whitespace, would undo the
               indentation and blank lines the system prompt asks the model to preserve. */
            return text.Trim('\r', '\n', ' ', '\t');
        }

        /// <summary>
        /// Concatenates the text blocks of a Messages reply. A reply is an array of content blocks
        /// and only the "text" ones carry the answer, so this skips anything else rather than
        /// indexing blindly into the first element.
        /// </summary>
        public static string ExtractText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                return null;
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    return null;

                var builder = new StringBuilder();

                foreach (var block in content.EnumerateArray())
                {
                    if (block.ValueKind != JsonValueKind.Object) continue;

                    if (!block.TryGetProperty("type", out var type) || type.GetString() != "text") continue;
                    if (!block.TryGetProperty("text", out var blockText) || blockText.ValueKind != JsonValueKind.String) continue;

                    builder.Append(blockText.GetString());
                }

                return builder.Length == 0 ? null : builder.ToString();
            }
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";
    }
}
