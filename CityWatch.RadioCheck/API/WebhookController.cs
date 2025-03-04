using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.API
{    

    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        [HttpPost("jotform")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                // Ensure the request is multipart/form-data
                if (!Request.HasFormContentType)
                    return BadRequest("Invalid form-data request");

                var form = await Request.ReadFormAsync();

                // Extracting Submission ID
                string submissionID = form["submissionID"].ToString();
                if (string.IsNullOrEmpty(submissionID))
                {
                    submissionID = Guid.NewGuid().ToString(); // Fallback if missing
                }

                // Extract raw JSON from rawRequest
                string rawJson = form["rawRequest"];
                var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;

                // Define folder structure
                string submissionFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", submissionID);
                if (!Directory.Exists(submissionFolder))
                    Directory.CreateDirectory(submissionFolder);

                string logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
                string webhookFilePath = Path.Combine(submissionFolder, "webhook_test.txt");

                await System.IO.File.AppendAllTextAsync(webhookFilePath, rawJson + Environment.NewLine);
                WriteLog(logFilePath, $"Webhook received. Data saved for Submission ID: {submissionID}");

                // Process file URLs from JSON
                if (webhookData != null)
                {
                    await DownloadAllFiles(webhookData, submissionFolder, logFilePath);
                }

                return Ok(new { message = $"Webhook received. Files saved in uploads/{submissionID}/" });
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "webhook_log.txt");
                WriteLog(logPath, $"Error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory, string logFilePath)
        {
            foreach (var key in webhookData.Keys)
            {
                if (webhookData[key] is JArray fileArray)
                {
                    var fileUrls = fileArray.ToObject<List<string>>();
                    foreach (var fileUrl in fileUrls)
                    {
                        if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
                        {
                            await DownloadAndSaveFile(fileUrl, saveDirectory, logFilePath);
                        }
                    }
                }
            }
        }

        private async Task DownloadAndSaveFile(string fileUrl, string saveDirectory, string logFilePath)
        {
            try
            {
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);

                string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                string filePath = Path.Combine(saveDirectory, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
                WriteLog(logFilePath, $"File downloaded: {fileName} from {fileUrl}");
            }
            catch (Exception ex)
            {
                WriteLog(logFilePath, $"Error downloading file from {fileUrl}: {ex.Message}");
            }
        }

        private void WriteLog(string logFilePath, string logMessage)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine($"{DateTime.UtcNow}: {logMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }
    }


}
