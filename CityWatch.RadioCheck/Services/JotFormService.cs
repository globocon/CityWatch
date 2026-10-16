using CityWatch.RadioCheck.Models.JotForm;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Dropbox.Api.Files.SearchMatchType;

namespace CityWatch.RadioCheck.Services
{

    public interface IJotFormService
    {
        Task<Dictionary<string, object>> GetFormNameFromJotForm(string _formId);
        Task<Dictionary<string, object>> GetFormFieldsAsync(string formId);
        Task<List<JotFormSubmission>> GetSubmissionsAsync(string formId);
        Task<bool> DeleteSubmissionAsync(string submissionId);
        Task<bool> UpdateSubmissionAsync(string submissionId, Dictionary<string, string> data);
        Task<bool> CreateSubmissionAsync(string formId, Dictionary<string, string> data);
    }


    public class JotFormService : IJotFormService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public JotFormService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _apiKey = _configuration["jotformSettings:ApiKey"];
            _apiUrl = _configuration["jotformSettings:ApiUrl"];
        }

        public async Task<Dictionary<string, object>> GetFormNameFromJotForm(string _formId)
        {
            var url = $"{_apiUrl}/form/{_formId}?apiKey={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var formResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            return formResponse?? new Dictionary<string, object>();            
        }

        //public async Task<Dictionary<string, object>> GetFormFieldsAsync(string _formId)
        //{
        //    var url = $"https://api.jotform.com/form/{_formId}/questions?apiKey={_apiKey}";
        //    var response = await _httpClient.GetStringAsync(url); // await _httpClient.GetFromJsonAsync<JotFormQuestionResponse>(url);
        //    var formResponse = JsonConvert.DeserializeObject<Dictionary<string, object>> (response);
        //    return formResponse?? new Dictionary<string, object>();
        //}

        public async Task<Dictionary<string, object>> GetFormFieldsAsync(string formId)
        {
            var url = $"{_apiUrl}/form/{formId}/questions?apiKey={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);

            // Deserialize into a temporary wrapper
            var fullResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

            if (fullResponse != null && fullResponse.TryGetValue("content", out var contentObj))
            {
                // Convert "content" back into a dictionary
                var contentJson = JsonConvert.SerializeObject(contentObj);
                var contentDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(contentJson);

                return contentDict ?? new Dictionary<string, object>();
            }

            return new Dictionary<string, object>();
        }


        public async Task<List<JotFormSubmission>> GetSubmissionsAsync(string _formId)
        {
            var submissions = new List<JsonElement>();
            int limit = 1000; // max Jotform allows
            int offset = 0;
            bool moreRecords = true;
            List <JotFormSubmission> rtnjfs = new List<JotFormSubmission>();

            //string filter = "%7B%22status%3Aeq%22%3A%22ACTIVE%22%7D"; // URL-encoded {"status:eq":"ACTIVE"}
            //string filter = "%7B%22status%3Aeq%22%3A%22CUSTOM%22%7D"; // URL-encoded {"status:eq":"CUSTOM"}
            string filter = "%7B%22status%3Ane%22%3A%22DELETED%22%7D"; // URL-encoded {"status:ne":"DELETED"}

            while (moreRecords)
            {
                var url = $"{_apiUrl}/form/{_formId}/submissions?apiKey={_apiKey}&limit={limit}&offset={offset}&filter={filter}";
                var response = await _httpClient.GetStringAsync(url);
                var formResponse = JsonConvert.DeserializeObject<JotFormSubmissionResponse>(response);
                rtnjfs.AddRange(formResponse?.content);

                //using var doc = JsonDocument.Parse(response);
                //var content = doc.RootElement.GetProperty("content").EnumerateArray().ToList();

                int count = formResponse?.content?.Count ?? 0;
                moreRecords = count == limit; // If fewer than limit, no more pages
                offset += limit;
            }
            rtnjfs = rtnjfs
            .Where(sub => sub.answers != null && sub.answers.Count > 0)
            .ToList();

            return rtnjfs;
        }

        public async Task<bool> DeleteSubmissionAsync(string submissionId)
        {
            var url = $"{_apiUrl}/submission/{submissionId}?apiKey={_apiKey}&_method=DELETE";
            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSubmissionAsync(string submissionId, Dictionary<string, string> data)
        {
            var url = $"{_apiUrl}/submission/{submissionId}?apiKey={_apiKey}&_method=PUT";
            var content = new FormUrlEncodedContent(data);
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateSubmissionAsync(string _formId, Dictionary<string, string> data)
        {
            var url = $"{_apiUrl}/form/{_formId}/submissions?apiKey={_apiKey}";
            var content = new FormUrlEncodedContent(data);
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
    }

}
