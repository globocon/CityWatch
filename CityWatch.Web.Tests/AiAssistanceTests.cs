using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Covers the AI Assistance services behind the incident text field on /Incident/Register.
    ///
    /// No network: both services are driven through a stub HttpMessageHandler, so nothing reaches
    /// LanguageTool or the AI provider and no key is needed to run these.
    /// </summary>
    [TestClass]
    public class AiAssistanceTests
    {
        /* ---------------- LanguageTool ---------------- */

        private const string TwoMatchResponse = @"{
            ""language"": { ""name"": ""English (Australian)"" },
            ""matches"": [
                { ""message"": ""Possible verb agreement error"", ""shortMessage"": ""Grammar"",
                  ""offset"": 6, ""length"": 6, ""replacements"": [ { ""value"": ""attended"" } ],
                  ""rule"": { ""id"": ""AGREEMENT"", ""category"": { ""name"": ""Grammar"" } } },
                { ""message"": ""Wrong tense"", ""offset"": 22, ""length"": 3,
                  ""replacements"": [ { ""value"": ""saw"" } ],
                  ""rule"": { ""id"": ""TENSE"", ""category"": { ""name"": ""Grammar"" } } }
            ] }";

        private static LanguageToolService LanguageTool(HttpStatusCode status, string body, Exception throws = null)
        {
            var handler = new StubHandler(status, body, throws);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.languagetool.org/") };
            return new LanguageToolService(client, NullLogger<LanguageToolService>.Instance);
        }

        [TestMethod]
        public async Task GrammarCheck_MapsMatchesAndDetectedLanguage()
        {
            //          0123456789...
            var text = "Guard attend site and see a person";
            var result = await LanguageTool(HttpStatusCode.OK, TwoMatchResponse).CheckAsync(text);

            Assert.AreEqual("English (Australian)", result.DetectedLanguage);
            Assert.AreEqual(2, result.Matches.Count);

            var first = result.Matches[0];
            Assert.AreEqual("attend", first.Original, "The UI shows what the offset points at.");
            Assert.AreEqual("attended", first.Replacements.Single());
            Assert.AreEqual("AGREEMENT", first.RuleId);
            Assert.AreEqual("Grammar", first.Category);

            // Guards the fixture itself: an offset that does not line up with the sentence would
            // otherwise silently test the wrong substring.
            Assert.AreEqual("see", result.Matches[1].Original);
        }

        /// <summary>
        /// The source language must be detected, not assumed. The old implementation hard-coded
        /// en-US, which mangled the Malayalam, Hindi and Tamil reports guards actually write.
        /// </summary>
        [TestMethod]
        public async Task GrammarCheck_AsksLanguageToolToDetectTheLanguage()
        {
            var handler = new StubHandler(HttpStatusCode.OK, TwoMatchResponse);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.languagetool.org/") };

            await new LanguageToolService(client, NullLogger<LanguageToolService>.Instance)
                .CheckAsync("Guard attend site and see a person");

            StringAssert.Contains(handler.LastRequestBody, "language=auto");
            Assert.IsFalse(handler.LastRequestBody.Contains("en-US"), "The language must not be hard-coded.");
        }

        [TestMethod]
        public async Task GrammarCheck_SuggestedTextAppliesEveryCorrectionAtTheRightOffsets()
        {
            var text = "Guard attend site and see a person";
            var result = await LanguageTool(HttpStatusCode.OK, TwoMatchResponse).CheckAsync(text);

            // "attend" -> "attended" lengthens the string; "see" -> "saw" must still land correctly.
            Assert.AreEqual("Guard attended site and saw a person", result.SuggestedText);
            Assert.AreEqual(text, result.OriginalText, "The original must survive untouched for the review step.");
        }

        [TestMethod]
        public async Task GrammarCheck_WithNoMatches_ReturnsTheTextUnchanged()
        {
            var result = await LanguageTool(HttpStatusCode.OK, @"{ ""matches"": [] }")
                .CheckAsync("The guard attended the site.");

            Assert.AreEqual(0, result.Matches.Count);
            Assert.AreEqual("The guard attended the site.", result.SuggestedText);
        }

        /// <summary>A match with no replacement must be reported, not silently dropped or crashed on.</summary>
        [TestMethod]
        public async Task GrammarCheck_MatchWithoutAReplacement_IsKeptButNotApplied()
        {
            var body = @"{ ""matches"": [ { ""message"": ""Unclear"", ""offset"": 0, ""length"": 5, ""replacements"": [] } ] }";
            var result = await LanguageTool(HttpStatusCode.OK, body).CheckAsync("Guard attended site");

            Assert.AreEqual(1, result.Matches.Count);
            Assert.AreEqual(0, result.Matches[0].Replacements.Count);
            Assert.AreEqual("Guard attended site", result.SuggestedText);
        }

        /// <summary>A bad offset from the provider must not throw out of a substring call.</summary>
        [TestMethod]
        public async Task GrammarCheck_MatchOutsideTheText_IsIgnored()
        {
            var body = @"{ ""matches"": [ { ""offset"": 500, ""length"": 10, ""replacements"": [ { ""value"": ""x"" } ] } ] }";
            var result = await LanguageTool(HttpStatusCode.OK, body).CheckAsync("Short text");

            Assert.AreEqual(0, result.Matches.Count);
            Assert.AreEqual("Short text", result.SuggestedText);
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        public async Task GrammarCheck_ProviderFailure_RaisesTheUnavailableError(HttpStatusCode status)
        {
            await Assert.ThrowsExceptionAsync<LanguageToolUnavailableException>(
                () => LanguageTool(status, "server exploded").CheckAsync("Some text"));
        }

        [TestMethod]
        public async Task GrammarCheck_NetworkFailure_RaisesTheUnavailableError()
        {
            await Assert.ThrowsExceptionAsync<LanguageToolUnavailableException>(
                () => LanguageTool(HttpStatusCode.OK, null, new HttpRequestException("no route to host"))
                        .CheckAsync("Some text"));
        }

        [TestMethod]
        public async Task GrammarCheck_UnreadableResponse_RaisesTheUnavailableError()
        {
            await Assert.ThrowsExceptionAsync<LanguageToolUnavailableException>(
                () => LanguageTool(HttpStatusCode.OK, "<html>not json</html>").CheckAsync("Some text"));
        }

        /* ---------------- AI provider (Claude) ---------------- */

        // An Anthropic Messages reply: an array of content blocks, only the "text" ones carry the answer.
        private const string MessagesApiBody = @"{
            ""content"": [ { ""type"": ""text"", ""text"": ""The guard attended the site."" } ],
            ""stop_reason"": ""end_turn"" }";

        private static AiService Ai(HttpStatusCode status, string body, string apiKey = "test-key",
            bool enabled = true, Exception throws = null, StubHandler handler = null)
        {
            handler ??= new StubHandler(status, body, throws);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
            var options = Options.Create(new AiOptions
            {
                ApiKey = apiKey,
                Enabled = enabled,
                Model = "claude-haiku-4-5",
                MaxTokensPerTurn = 4096
            });
            return new AiService(client, options, NullLogger<AiService>.Instance);
        }

        /// <summary>
        /// The reported symptom: with no key configured the old code called the provider anyway,
        /// turned the 401 into the string "OpenAI error" and the page wrote that over the guard's
        /// incident report. It must fail before any request is made.
        /// </summary>
        [TestMethod]
        public async Task Ai_WithNoApiKey_FailsWithoutCallingTheProvider()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            var service = Ai(HttpStatusCode.OK, MessagesApiBody, apiKey: "", handler: handler);

            Assert.IsFalse(service.IsConfigured);
            await Assert.ThrowsExceptionAsync<AiServiceUnavailableException>(
                () => service.ImproveIncidentReportAsync("Person entered site."));

            Assert.AreEqual(0, handler.CallCount, "No request may be made without a key.");
        }

        /// <summary>The master switch has to stop the feature even when a key is present.</summary>
        [TestMethod]
        public async Task Ai_WhenDisabled_FailsWithoutCallingTheProvider()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            var service = Ai(HttpStatusCode.OK, MessagesApiBody, enabled: false, handler: handler);

            Assert.IsFalse(service.IsConfigured);
            await Assert.ThrowsExceptionAsync<AiServiceUnavailableException>(
                () => service.TranslateAsync("Text", "Italian"));

            Assert.AreEqual(0, handler.CallCount);
        }

        /// <summary>
        /// The anti-hallucination instructions used to be built into a local variable and then never
        /// referenced - the provider received the raw text with no instructions at all.
        /// </summary>
        [TestMethod]
        public async Task Improve_SendsTheAntiHallucinationInstructionsAsTheSystemPrompt()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            await Ai(HttpStatusCode.OK, MessagesApiBody, handler: handler)
                .ImproveIncidentReportAsync("guard see person enter site and tell him leave");

            var sent = handler.LastRequestBody;
            StringAssert.Contains(sent, "system", "The instructions must actually be on the request.");
            StringAssert.Contains(sent, "must NOT invent");
            StringAssert.Contains(sent, "Preserve every factual detail");
            StringAssert.Contains(sent, "guard see person enter site and tell him leave");
        }

        /// <summary>Formatting the guard typed must survive the rewrite, so the prompt has to ask for it.</summary>
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Ai_InstructsTheModelToPreserveFormatting(bool improve)
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            var service = Ai(HttpStatusCode.OK, MessagesApiBody, handler: handler);

            if (improve)
                await service.ImproveIncidentReportAsync("Line one.\n\n- point");
            else
                await service.TranslateAsync("Line one.\n\n- point", "Italian");

            StringAssert.Contains(handler.LastRequestBody, "Preserve the original formatting");
        }

        [TestMethod]
        public async Task Translate_SendsATranslationInstructionNamingTheTarget()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            await Ai(HttpStatusCode.OK, MessagesApiBody, handler: handler)
                .TranslateAsync("The guard attended the site.", "Australian English");

            var sent = handler.LastRequestBody;
            StringAssert.Contains(sent, "Australian English");
            StringAssert.Contains(sent, "Do not add information");
            Assert.IsFalse(sent.Contains("Expand short descriptions"),
                "Translation must not reuse the rewrite instruction, or it changes two things at once.");
        }

        /// <summary>Claude authenticates with x-api-key plus a pinned wire version, not a Bearer token.</summary>
        [TestMethod]
        public async Task Ai_SendsTheAnthropicAuthenticationHeaders()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            await Ai(HttpStatusCode.OK, MessagesApiBody, handler: handler).ImproveIncidentReportAsync("Text");

            Assert.AreEqual("test-key", handler.LastHeader("x-api-key"));
            Assert.AreEqual("2023-06-01", handler.LastHeader("anthropic-version"));
            Assert.IsNull(handler.LastAuthorizationScheme, "Anthropic does not use an Authorization header.");
        }

        [TestMethod]
        public async Task Ai_SendsTheConfiguredModelAndTokenCap()
        {
            var handler = new StubHandler(HttpStatusCode.OK, MessagesApiBody);
            await Ai(HttpStatusCode.OK, MessagesApiBody, handler: handler).ImproveIncidentReportAsync("Text");

            StringAssert.Contains(handler.LastRequestBody, "claude-haiku-4-5");
            StringAssert.Contains(handler.LastRequestBody, "max_tokens");
            Assert.AreEqual("/v1/messages", handler.LastRequestPath);
        }

        [TestMethod]
        public async Task Ai_ProviderError_RaisesTheUnavailableErrorRatherThanReturningErrorText()
        {
            // The old code returned the string "OpenAI error", which the page then saved as the report.
            var ex = await Assert.ThrowsExceptionAsync<AiServiceUnavailableException>(
                () => Ai(HttpStatusCode.Unauthorized, @"{ ""error"": { ""message"": ""bad key"" } }")
                        .ImproveIncidentReportAsync("Text"));

            Assert.IsFalse(ex.Message.Contains("bad key"), "Provider detail belongs in the log, not the message.");
        }

        [TestMethod]
        public async Task Ai_EmptyProviderOutput_RaisesTheUnavailableError()
        {
            await Assert.ThrowsExceptionAsync<AiServiceUnavailableException>(
                () => Ai(HttpStatusCode.OK, @"{ ""content"": [] }").ImproveIncidentReportAsync("Text"));
        }

        [TestMethod]
        public async Task Ai_NetworkFailure_RaisesTheUnavailableError()
        {
            await Assert.ThrowsExceptionAsync<AiServiceUnavailableException>(
                () => Ai(HttpStatusCode.OK, null, throws: new HttpRequestException("no route to host"))
                        .ImproveIncidentReportAsync("Text"));
        }

        /// <summary>Line breaks in the model's answer must reach the field intact.</summary>
        [TestMethod]
        public async Task Ai_KeepsLineBreaksInTheReply()
        {
            var body = @"{ ""content"": [ { ""type"": ""text"", ""text"": ""Line one.\n\n- point one\n- point two"" } ] }";
            var result = await Ai(HttpStatusCode.OK, body).ImproveIncidentReportAsync("Text");

            Assert.AreEqual("Line one.\n\n- point one\n- point two", result);
        }

        /// <summary>A reply is an array of blocks; only the text ones carry the answer.</summary>
        [TestMethod]
        public void ExtractText_SkipsNonTextContentBlocks()
        {
            var body = @"{ ""content"": [
                { ""type"": ""thinking"", ""thinking"": ""ignored"" },
                { ""type"": ""text"", ""text"": ""Result text."" } ] }";

            Assert.AreEqual("Result text.", AiService.ExtractText(body));
        }

        [TestMethod]
        public void ExtractText_HandlesGarbageWithoutThrowing()
        {
            Assert.IsNull(AiService.ExtractText(@"{ ""unexpected"": true }"));
            Assert.IsNull(AiService.ExtractText("<html>not json</html>"));
            Assert.IsNull(AiService.ExtractText(""));
        }

        /* ---------------- configuration ---------------- */

        /// <summary>The language list is served from configuration rather than hard-coded in JavaScript.</summary>
        [TestMethod]
        public void Settings_CarryTheTargetLanguagesTheConverterOffers()
        {
            var settings = new AiAssistanceSettings
            {
                TargetLanguages = new List<AiLanguageOption>
                {
                    new AiLanguageOption { Code = "en-AU", Name = "Australian English" },
                    new AiLanguageOption { Code = "ml", Name = "Malayalam" }
                }
            };

            Assert.AreEqual("Australian English",
                settings.TargetLanguages.Single(z => z.Code == "en-AU").Name);
            Assert.AreEqual(10000, new AiAssistanceSettings().MaxTextLength, "A default cap must exist.");
        }

        /* ---------------- stub transport ---------------- */

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            private readonly Exception _throws;

            public StubHandler(HttpStatusCode status, string body, Exception throws = null)
            {
                _status = status;
                _body = body;
                _throws = throws;
            }

            public int CallCount { get; private set; }
            public string LastRequestBody { get; private set; }
            public string LastRequestPath { get; private set; }
            public string LastAuthorizationScheme { get; private set; }
            public string LastAuthorizationParameter { get; private set; }

            private readonly Dictionary<string, string> _headers = new();
            public string LastHeader(string name) => _headers.TryGetValue(name, out var value) ? value : null;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                LastRequestPath = request.RequestUri?.AbsolutePath;
                LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
                LastAuthorizationParameter = request.Headers.Authorization?.Parameter;

                _headers.Clear();
                foreach (var header in request.Headers)
                    _headers[header.Key] = string.Join(",", header.Value);

                if (_throws != null)
                    throw _throws;

                return new HttpResponseMessage(_status) { Content = new StringContent(_body ?? string.Empty) };
            }
        }
    }
}
