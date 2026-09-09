using System.Collections.Generic;

namespace CityWatch.Web.Models
{
    /* ---------------------------------------------------------------------------------------
       Contracts for the AI Assistance feature on /Incident/Register.

       Nothing here mirrors an external provider's response shape on purpose: LanguageTool and
       the LLM are both isolated behind ILanguageToolService / IAiService so the provider can be
       swapped (self-hosted LanguageTool, a different LLM) without the Incident Register UI
       changing. The browser only ever sees these types.
       --------------------------------------------------------------------------------------- */

    /// <summary>
    /// AI platform configuration, bound from the "Ai" section - the same section name, provider and
    /// settings shape the SmartRosterAI platform uses, so a deployment configures both the same way.
    /// The API key must NEVER live in a committed appsettings file: use user-secrets in development
    /// and an environment variable or key vault in production.
    /// </summary>
    public class AiOptions
    {
        public const string SectionName = "Ai";

        /// <summary>Model provider. "Anthropic" is the implemented one.</summary>
        public string Provider { get; set; } = "Anthropic";

        public string ApiKey { get; set; } = "";

        public string BaseAddress { get; set; } = "https://api.anthropic.com/";

        /// <summary>
        /// The model used for AI Assistance. This is a single-shot text transform rather than an
        /// agent turn, so it runs on the cheap, fast model rather than the conversational one.
        /// </summary>
        public string Model { get; set; } = "claude-haiku-4-5";

        /// <summary>Hard cap on output tokens per call.</summary>
        public int MaxTokensPerTurn { get; set; } = 8192;

        /// <summary>Per-model-call timeout.</summary>
        public int RequestTimeoutSeconds { get; set; } = 120;

        /// <summary>Master switch - lets ops disable AI assistance instantly.</summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>Configuration for the AI Assistance feature - <c>appsettings.json</c> section "AiAssistance".</summary>
    public class AiAssistanceSettings
    {
        public const string Name = "AiAssistance";

        /// <summary>
        /// The public LanguageTool service by default. Point this at a self-hosted or premium
        /// instance if C4i usage outgrows the free tier - no other code has to change.
        /// </summary>
        public string LanguageToolBaseAddress { get; set; } = "https://api.languagetool.org/";

        /// <summary>Guards against a paste of an entire document running up an API bill.</summary>
        public int MaxTextLength { get; set; } = 10000;

        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Offered in the Language Converter dropdown. Served to the browser rather than hard-coded
        /// in JavaScript, so the list can be changed without a deployment.
        /// </summary>
        public List<AiLanguageOption> TargetLanguages { get; set; } = new();
    }

    public class AiLanguageOption
    {
        /// <summary>Sent back by the browser and validated server-side against this list.</summary>
        public string Code { get; set; }

        /// <summary>What the guard sees, e.g. "Australian English".</summary>
        public string Name { get; set; }
    }

    public class AiTextRequest
    {
        public string Text { get; set; }
    }

    public class AiTranslationRequest
    {
        public string Text { get; set; }
        public string TargetLanguage { get; set; }
    }

    /// <summary>Result of a rewrite or a conversion. The caller decides whether to keep it.</summary>
    public class AiTextResult
    {
        public string OriginalText { get; set; }
        public string ResultText { get; set; }
        public string DetectedLanguage { get; set; }
    }

    public class GrammarCheckResult
    {
        public string OriginalText { get; set; }

        /// <summary>What LanguageTool detected, e.g. "English (Australian)". Never assumed.</summary>
        public string DetectedLanguage { get; set; }

        public List<GrammarMatch> Matches { get; set; } = new();

        /// <summary>
        /// The original text with the first suggested replacement applied to every match. Offered
        /// as a preview for the guard to accept or reject - it is never written to the field on
        /// their behalf.
        /// </summary>
        public string SuggestedText { get; set; }
    }

    /// <summary>One LanguageTool finding, trimmed to what the UI actually renders.</summary>
    public class GrammarMatch
    {
        public string Message { get; set; }
        public string ShortMessage { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }

        /// <summary>The text the offset/length points at, so the UI can show "was" and "suggested".</summary>
        public string Original { get; set; }

        public List<string> Replacements { get; set; } = new();
        public string RuleId { get; set; }
        public string Category { get; set; }
    }
}
