using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using static Dropbox.Api.Files.SearchMatchType;
using Microsoft.Extensions.Configuration;

namespace CityWatch.Web.Services
{
    public class AiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public AiService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _apiKey = configuration["ApiSettings:ApiKey"];
        }

        public async Task<string> TestAsync(string input)
        {
           
            
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var prompt = $@"
You are an assistant that improves incident reports.

STRICT RULES:
- Use ONLY the information provided
- Do NOT add, assume, or invent new details
- Do NOT remove any facts
- Correct grammar and spelling
- Keep a professional incident report tone
- DO NOT include any preamble, greetings, or instructions
- DO NOT ask for placeholders or further customization

FORMAT LOGIC (VERY IMPORTANT):
- If the content is a list of facts, items, or evidence → output bullet points ONLY
- If the content is a continuous narrative → output ONE paragraph ONLY
- If the content contains multiple actions or steps AND benefits from a summary → output bullet points FIRST, then ONE paragraph summarising the same content
- Decide the format automatically based on the content
- Do NOT force bullets or paragraphs if they are not suitable

TEXT:
{input}

OUTPUT:
";


            var body = new
            {
                model = "gpt-4.1-mini",
                input = input
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                "https://api.openai.com/v1/responses", content);

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);

            // Safety check
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                return "OpenAI error";
            }

            // Navigate to output text
            var text = doc.RootElement
                .GetProperty("output")[0]
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return text;
        }
    }
}
