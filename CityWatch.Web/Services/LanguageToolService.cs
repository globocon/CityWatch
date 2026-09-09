using CityWatch.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// Grammar, spelling and style checking. LanguageTool today; the interface exists so a
    /// self-hosted or premium instance can replace it without the Incident Register changing.
    /// </summary>
    public interface ILanguageToolService
    {
        Task<GrammarCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default);
    }

    /// <summary>Raised when the grammar provider cannot be reached or answers with something unusable.</summary>
    public class LanguageToolUnavailableException : Exception
    {
        public LanguageToolUnavailableException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class LanguageToolService : ILanguageToolService
    {
        private readonly HttpClient _http;
        private readonly ILogger<LanguageToolService> _logger;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LanguageToolService(HttpClient http, ILogger<LanguageToolService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<GrammarCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default)
        {
            /* language=auto, never a hard-coded en-US. Guards write in Malayalam, Hindi, Tamil and
               Italian among others, and checking those against American English produced nonsense. */
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("language", "auto")
            });

            LanguageToolResponse response;
            try
            {
                var httpResponse = await _http.PostAsync("v2/check", form, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // The body can carry a rate-limit or validation message; useful in the log, never
                    // shown to the guard.
                    var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("LanguageTool request failed with {StatusCode}. Body: {Body}",
                        (int)httpResponse.StatusCode, Truncate(body, 500));
                    throw new LanguageToolUnavailableException($"LanguageTool returned {(int)httpResponse.StatusCode}.");
                }

                var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                response = JsonSerializer.Deserialize<LanguageToolResponse>(json, SerializerOptions);
            }
            catch (LanguageToolUnavailableException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("LanguageTool request timed out.");
                throw new LanguageToolUnavailableException("LanguageTool timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LanguageTool request failed.");
                throw new LanguageToolUnavailableException("LanguageTool could not be reached.", ex);
            }

            if (response == null)
                throw new LanguageToolUnavailableException("LanguageTool returned an unreadable response.");

            var matches = (response.Matches ?? new List<LanguageToolMatch>())
                .Where(m => m != null && m.Offset >= 0 && m.Length > 0 && m.Offset + m.Length <= text.Length)
                .OrderBy(m => m.Offset)
                .Select(m => new GrammarMatch
                {
                    Message = m.Message,
                    ShortMessage = m.ShortMessage,
                    Offset = m.Offset,
                    Length = m.Length,
                    Original = text.Substring(m.Offset, m.Length),
                    // A handful is all the UI shows; LanguageTool can return dozens per match.
                    Replacements = (m.Replacements ?? new List<LanguageToolReplacement>())
                        .Where(r => r?.Value != null).Select(r => r.Value).Take(5).ToList(),
                    RuleId = m.Rule?.Id,
                    Category = m.Rule?.Category?.Name
                })
                .ToList();

            return new GrammarCheckResult
            {
                OriginalText = text,
                DetectedLanguage = response.Language?.Name ?? response.Language?.DetectedLanguage?.Name,
                Matches = matches,
                SuggestedText = ApplySuggestions(text, matches)
            };
        }

        /// <summary>
        /// Applies the first replacement of every match, walking backwards so earlier offsets stay
        /// valid without tracking a running adjustment. Overlapping matches are skipped rather than
        /// corrupting the text. This is a preview only - the guard decides whether to use it.
        /// </summary>
        internal static string ApplySuggestions(string text, List<GrammarMatch> matches)
        {
            if (string.IsNullOrEmpty(text) || matches == null || matches.Count == 0)
                return text;

            var result = text;
            var nextStart = text.Length;

            foreach (var match in matches.OrderByDescending(m => m.Offset))
            {
                if (match.Replacements.Count == 0)
                    continue;

                // Skip a match that overlaps the one already applied to its right.
                if (match.Offset + match.Length > nextStart)
                    continue;

                result = result.Substring(0, match.Offset)
                         + match.Replacements[0]
                         + result.Substring(match.Offset + match.Length);

                nextStart = match.Offset;
            }

            return result;
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";

        /* ---- LanguageTool wire format. Internal: nothing outside this file sees it. ---- */

        private class LanguageToolResponse
        {
            [JsonPropertyName("language")]
            public LanguageToolLanguage Language { get; set; }

            [JsonPropertyName("matches")]
            public List<LanguageToolMatch> Matches { get; set; }
        }

        private class LanguageToolLanguage
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("detectedLanguage")]
            public LanguageToolLanguage DetectedLanguage { get; set; }
        }

        private class LanguageToolMatch
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("shortMessage")]
            public string ShortMessage { get; set; }

            [JsonPropertyName("offset")]
            public int Offset { get; set; }

            [JsonPropertyName("length")]
            public int Length { get; set; }

            [JsonPropertyName("replacements")]
            public List<LanguageToolReplacement> Replacements { get; set; }

            [JsonPropertyName("rule")]
            public LanguageToolRule Rule { get; set; }
        }

        private class LanguageToolReplacement
        {
            [JsonPropertyName("value")]
            public string Value { get; set; }
        }

        private class LanguageToolRule
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("category")]
            public LanguageToolCategory Category { get; set; }
        }

        private class LanguageToolCategory
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }
}
