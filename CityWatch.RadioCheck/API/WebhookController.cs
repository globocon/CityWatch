using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.API
{

    //[Route("api/webhook")]
    //[ApiController]
    //public class WebhookController : ControllerBase
    //{
    //    [HttpPost("jotform")]
    //    public async Task<IActionResult> ReceiveWebhook()
    //    {
    //        try
    //        {
    //            // Ensure the request is multipart/form-data
    //            if (!Request.HasFormContentType)
    //                return BadRequest("Invalid form-data request");

    //            var form = await Request.ReadFormAsync();

    //            // Extracting Submission ID
    //            string submissionID = form["submissionID"].ToString();
    //            if (string.IsNullOrEmpty(submissionID))
    //            {
    //                submissionID = Guid.NewGuid().ToString(); // Fallback if missing
    //            }

    //            // Extract raw JSON from rawRequest
    //            string rawJson = form["rawRequest"];
    //            var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;

    //            // Define folder structure
    //            string submissionFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", submissionID);
    //            if (!Directory.Exists(submissionFolder))
    //                Directory.CreateDirectory(submissionFolder);

    //            string logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
    //            string webhookFilePath = Path.Combine(submissionFolder, "webhook_test.txt");

    //            await System.IO.File.AppendAllTextAsync(webhookFilePath, rawJson + Environment.NewLine);
    //            WriteLog(logFilePath, $"Webhook received. Data saved for Submission ID: {submissionID}");

    //            // Process file URLs from JSON
    //            if (webhookData != null)
    //            {
    //                await DownloadAllFiles(webhookData, submissionFolder, logFilePath);
    //            }

    //            return Ok(new { message = $"Webhook received. Files saved in uploads/{submissionID}/" });
    //        }
    //        catch (Exception ex)
    //        {
    //            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "webhook_log.txt");
    //            WriteLog(logPath, $"Error: {ex.Message}");
    //            return StatusCode(500, $"Error: {ex.Message}");
    //        }
    //    }

    //    private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory, string logFilePath)
    //    {
    //        foreach (var key in webhookData.Keys)
    //        {
    //            if (webhookData[key] is JArray fileArray)
    //            {
    //                var fileUrls = fileArray.ToObject<List<string>>();
    //                foreach (var fileUrl in fileUrls)
    //                {
    //                    if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
    //                    {
    //                        await DownloadAndSaveFile(fileUrl, saveDirectory, logFilePath);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private async Task DownloadAndSaveFile(string fileUrl, string saveDirectory, string logFilePath)
    //    {
    //        try
    //        {
    //            using var httpClient = new HttpClient();
    //            var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);

    //            string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
    //            string filePath = Path.Combine(saveDirectory, fileName);

    //            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
    //            WriteLog(logFilePath, $"File downloaded: {fileName} from {fileUrl}");
    //        }
    //        catch (Exception ex)
    //        {
    //            WriteLog(logFilePath, $"Error downloading file from {fileUrl}: {ex.Message}");
    //        }
    //    }

    //    private void WriteLog(string logFilePath, string logMessage)
    //    {
    //        try
    //        {
    //            using (StreamWriter sw = new StreamWriter(logFilePath, true))
    //            {
    //                sw.WriteLine($"{DateTime.UtcNow}: {logMessage}");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Failed to write log: {ex.Message}");
    //        }
    //    }
    //}


    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        //private const string JotFormApiKey = "6a5b7d0e94fdac941d2f857a5f096e47";
        private const string JotFormApiKey = "4a0a2fe279684c4953af50311f5a2a93";
        public WebhookController()
        {
            _httpClient = new HttpClient();
        }

        [HttpPost("jotform")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                if (!Request.HasFormContentType)
                    return BadRequest("Invalid form-data request");

                var form = await Request.ReadFormAsync();
                string submissionID = form["submissionID"].ToString();
                if (string.IsNullOrEmpty(submissionID))
                {
                    submissionID = Guid.NewGuid().ToString();
                }

                string rawJson = form["rawRequest"];
                var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;
                string formID = "UnknownFormID";
                if (webhookData != null)
                {
                    if (webhookData.ContainsKey("path"))
                    {
                        var path = webhookData["path"].ToString();
                        var pathParts = path.Split('/');
                        formID = pathParts.Length > 2 ? pathParts[2] : "UnknownFormID";
                    }
                    else if (webhookData.ContainsKey("slug"))
                    {
                        var slug = webhookData["slug"].ToString();
                        var slugParts = slug.Split('/');
                        formID = slugParts.Length > 1 ? slugParts[1] : "UnknownFormID";
                    }
                }


                string formName = await GetFormNameFromJotForm(formID);
                //string workOrder = webhookData != null && webhookData.ContainsKey("q3_workOrder") ? webhookData["q3_workOrder"].ToString() : "UnknownWorkOrder";
                string workOrder = webhookData != null ? webhookData.FirstOrDefault(kvp => kvp.Key.Contains("_workOrder")).Value?.ToString() ?? "UnknownWorkOrder" : "UnknownWorkOrder";
                string submissionFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", formName, workOrder);
                if (!Directory.Exists(submissionFolder))
                    Directory.CreateDirectory(submissionFolder);

                string logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
                string webhookFilePath = Path.Combine(submissionFolder, "webhook_test.txt");
                string excelFilePath = Path.Combine(submissionFolder, "webhook_data.xlsx");
                string jsonFilePath = Path.Combine(submissionFolder, "image_captions.json");

                await System.IO.File.AppendAllTextAsync(webhookFilePath, rawJson + Environment.NewLine);
                WriteLog(logFilePath, $"Webhook received. Data saved for Submission ID: {submissionID}");

                if (webhookData != null)
                {
                    await DownloadAllFiles(webhookData, submissionFolder, logFilePath);
                    AppendToExcel(excelFilePath, webhookData);
                    // Save JSON to a file
                    string jsonOutput = GetImageNamesAndCaptionsJson(webhookData, logFilePath);
                    await System.IO.File.WriteAllTextAsync(jsonFilePath, jsonOutput);
                }

                return Ok(new { message = $"Webhook received. Files saved in uploads/jotform/{formName}/{workOrder}/" });
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "webhook_log.txt");
                WriteLog(logPath, $"Error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task<string> GetFormNameFromJotForm(string formID)
        {
            try
            {
                string url = $"https://api.jotform.com/form/{formID}?apiKey={JotFormApiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var formResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                if (formResponse != null && formResponse.ContainsKey("content"))
                {
                    var content = formResponse["content"] as JObject; // Correctly cast content as JObject
                    return content != null && content.ContainsKey("title") ? content["title"].ToString() : "UnknownForm";
                }
                return "UnknownForm";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching form name: {ex.Message}");
                return "UnknownForm";
            }
        }


        //private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory, string logFilePath)
        //{
        //    foreach (var key in webhookData.Keys)
        //    {
        //        if (webhookData[key] is JArray fileArray)
        //        {
        //            var fileUrls = fileArray.ToObject<List<string>>();
        //            foreach (var fileUrl in fileUrls)
        //            {
        //                if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
        //                {
        //                    await DownloadAndSaveFile(fileUrl, saveDirectory, logFilePath);
        //                }
        //            }
        //        }
        //    }
        //}


        //private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory, string logFilePath)
        //{
        //    foreach (var entry in webhookData)
        //    {
        //        if (entry.Value is JArray fileArray) // Check if value is a JArray (list of URLs)
        //        {
        //            await ProcessFileArray(fileArray, saveDirectory, logFilePath);
        //        }
        //        else if (entry.Value is JObject nestedObject) // Check for nested JSON objects
        //        {
        //            await DownloadAllFiles(nestedObject.ToObject<Dictionary<string, object>>(), saveDirectory, logFilePath);
        //        }
        //        else if (entry.Value is Dictionary<string, object> nestedDict) // Check for nested Dictionary
        //        {
        //            await DownloadAllFiles(nestedDict, saveDirectory, logFilePath);
        //        }
        //    }
        //}

        private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory, string logFilePath)
        {
            foreach (var entry in webhookData)
            {
                WriteLog(logFilePath, $"Processing key: {entry.Key}");

                if (entry.Value is JArray fileArray) // Direct file list
                {
                    await ProcessFileArray(fileArray, saveDirectory, logFilePath);
                }
                else if (entry.Value is JObject nestedObject) // Nested JSON object
                {
                    await DownloadAllFiles(nestedObject.ToObject<Dictionary<string, object>>(), saveDirectory, logFilePath);
                }
                else if (entry.Value is Dictionary<string, object> nestedDict) // Nested Dictionary
                {
                    // First process direct file lists inside the nested dictionary
                    foreach (var subEntry in nestedDict)
                    {
                        if (subEntry.Value is JArray subFileArray)
                        {
                            await ProcessFileArray(subFileArray, saveDirectory, logFilePath);
                        }
                    }
                    // Then, recursively process the nested dictionary
                    await DownloadAllFiles(nestedDict, saveDirectory, logFilePath);
                }
            }
        }

        /// <summary>
        /// Old Code its working but replace to handle the file url 
        /// </summary>
        /// <param name="fileArray"></param>
        /// <param name="saveDirectory"></param>
        /// <param name="logFilePath"></param>
        /// <returns></returns>
        //private async Task ProcessFileArray(JArray fileArray, string saveDirectory, string logFilePath)
        //{
        //    var fileUrls = fileArray.ToObject<List<string>>();
        //    foreach (var fileUrl in fileUrls)
        //    {
        //        if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
        //        {
        //            await DownloadAndSaveFile(fileUrl, saveDirectory, logFilePath);
        //        }
        //    }
        //}


        private async Task ProcessFileArray(JArray fileArray, string saveDirectory, string logFilePath)
        {
            var fileUrls = fileArray.ToObject<List<string>>();
            foreach (var fileUrl in fileUrls)
            {
                string normalizedUrl = Regex.Unescape(fileUrl).Replace("\\", "/").Trim();  // Unescape JSON & fix slashes

                if (Uri.IsWellFormedUriString(normalizedUrl, UriKind.Absolute))
                {
                    await DownloadAndSaveFile(normalizedUrl, saveDirectory, logFilePath);
                }
                else
                {
                    WriteLog(logFilePath, $"Invalid URL: {fileUrl}");
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


        private void AppendToExcel(string excelFilePath, Dictionary<string, object> webhookData)
        {
            bool fileExists = System.IO.File.Exists(excelFilePath);
            using (var workbook = fileExists ? new XLWorkbook(excelFilePath) : new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("WebhookData");

                // Determine the last used row (or set to 1 if empty)
                int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                // If file does not exist, write the headers
                if (!fileExists)
                {
                    int colIndex = 1;
                    foreach (var key in webhookData.Keys)
                    {
                        worksheet.Cell(1, colIndex).Value = key;
                        worksheet.Cell(1, colIndex).Style.Font.Bold = true;
                        colIndex++;
                    }
                }

                // Append the new data row
                int newRow = lastUsedRow + 1;
                int col = 1;
                foreach (var value in webhookData.Values)
                {
                    worksheet.Cell(newRow, col).Value = value?.ToString();
                    col++;
                }

                // Auto-fit columns for better readability
                worksheet.Columns().AdjustToContents();

                // Save the workbook
                workbook.SaveAs(excelFilePath);
            }
        }


        //public string GetImageNamesAndCaptionsJson(Dictionary<string, object> webhookData, string logFilePath)
        //{
        //    var imagesWithCaptions = new List<object>();

        //    try
        //    {
        //        System.IO.File.AppendAllText(logFilePath, "Processing Webhook Data...\n");

        //        foreach (var kvp in webhookData)
        //        {
        //            System.IO.File.AppendAllText(logFilePath, $"Processing key: {kvp.Key}\n");

        //            if (kvp.Key.Contains("_Photo") && kvp.Value is object value)
        //            {
        //                if (value is JArray array) // If multiple images exist
        //                {
        //                    int index = 1;
        //                    foreach (var item in array)
        //                    {
        //                        string imageUrl = item.ToString();
        //                        string imageName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

        //                        // Find the matching caption key
        //                        string captionKey = FindMatchingCaptionKey(webhookData, index, logFilePath);
        //                        string caption = webhookData.ContainsKey(captionKey) ? webhookData[captionKey].ToString() : "No caption";

        //                        // Log the caption status
        //                        System.IO.File.AppendAllText(logFilePath, $"Image: {imageName}, Caption Key: {captionKey}, Caption: {caption}\n");

        //                        imagesWithCaptions.Add(new { ImageName = imageName, Caption = caption });
        //                        index++;
        //                    }
        //                }
        //                else if (value is string imageUrl) // If a single image exists
        //                {
        //                    string imageName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

        //                    // Find the matching caption key
        //                    string captionKey = FindMatchingCaptionKey(webhookData, 1, logFilePath); // Use index 1 for the first image
        //                    string caption = webhookData.ContainsKey(captionKey) ? webhookData[captionKey].ToString() : "No caption";

        //                    // Log the caption status
        //                    System.IO.File.AppendAllText(logFilePath, $"Image: {imageName}, Caption Key: {captionKey}, Caption: {caption}\n");

        //                    imagesWithCaptions.Add(new { ImageName = imageName, Caption = caption });
        //                }
        //            }
        //        }

        //        string jsonOutput = JsonConvert.SerializeObject(imagesWithCaptions, Formatting.Indented);
        //        System.IO.File.AppendAllText(logFilePath, "Final JSON Output:\n" + jsonOutput + "\n");

        //        return jsonOutput;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.IO.File.AppendAllText(logFilePath, "Error: " + ex.Message + "\n");
        //        return "Error occurred. Check log file.";
        //    }
        //}
        //private string FindMatchingCaptionKey(Dictionary<string, object> webhookData, int photoIndex, string logFilePath)
        //{
        //    // Construct the caption key based on the index
        //    string captionKey = $"_photoCaption{photoIndex}";

        //    // Log the key checking process
        //    System.IO.File.AppendAllText(logFilePath, $"Checking Caption Key: {captionKey}\n");

        //    // Iterate over each key in the webhookData
        //    foreach (var key in webhookData.Keys)
        //    {
        //        // Check if the key ends with the constructed captionKey (suffix match)
        //        if (key.EndsWith(captionKey, StringComparison.OrdinalIgnoreCase))
        //        {
        //            return key;  // Return the valid caption key
        //        }
        //    }

        //    // Return null if no matching key found
        //    return null;
        //}


        public string GetImageNamesAndCaptionsJson(Dictionary<string, object> webhookData, string logFilePath)
        {
            var imagesWithCaptions = new List<object>();

            try
            {
                System.IO.File.AppendAllText(logFilePath, "Processing Webhook Data...\n");

                foreach (var kvp in webhookData)
                {
                    System.IO.File.AppendAllText(logFilePath, $"Processing key: {kvp.Key}\n");

                    if (kvp.Key.Contains("_Photo") && kvp.Value is object value)
                    {
                        if (value is JArray array) // If multiple images exist
                        {
                            foreach (var item in array)
                            {
                                string imageUrl = item.ToString();
                                string imageName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

                                // Find the matching caption key
                                string captionKey = FindMatchingCaptionKey(webhookData, kvp.Key, logFilePath);
                                string caption = captionKey != null && webhookData.ContainsKey(captionKey)
                                    ? webhookData[captionKey].ToString()
                                    : "No caption";

                                // Log the caption status
                                System.IO.File.AppendAllText(logFilePath, $"Image: {imageName}, Caption Key: {captionKey}, Caption: {caption}\n");

                                imagesWithCaptions.Add(new { ImageName = imageName, Caption = caption });
                            }
                        }
                        else if (value is string imageUrl) // If a single image exists
                        {
                            string imageName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

                            // Find the matching caption key
                            string captionKey = FindMatchingCaptionKey(webhookData, kvp.Key, logFilePath);
                            string caption = captionKey != null && webhookData.ContainsKey(captionKey)
                                ? webhookData[captionKey].ToString()
                                : "No caption";

                            // Log the caption status
                            System.IO.File.AppendAllText(logFilePath, $"Image: {imageName}, Caption Key: {captionKey}, Caption: {caption}\n");

                            imagesWithCaptions.Add(new { ImageName = imageName, Caption = caption });
                        }
                    }
                }

                string jsonOutput = JsonConvert.SerializeObject(imagesWithCaptions, Formatting.Indented);
                System.IO.File.AppendAllText(logFilePath, "Final JSON Output:\n" + jsonOutput + "\n");

                return jsonOutput;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logFilePath, "Error: " + ex.Message + "\n");
                return "Error occurred. Check log file.";
            }
        }

        private string FindMatchingCaptionKey(Dictionary<string, object> webhookData, string photoKey, string logFilePath)
        {
            // Extract the number from the _Photo key (e.g., "01TrackInspector_ExpectedMaterial_Photo2" -> "2")
            var match = Regex.Match(photoKey, @"\d+$");
            if (!match.Success)
            {
                System.IO.File.AppendAllText(logFilePath, $"No valid index found for {photoKey}\n");
                return null;
            }

            string expectedCaptionSuffix = $"_photoCaption{match.Value}"; // e.g., "_photoCaption2"

            System.IO.File.AppendAllText(logFilePath, $"Looking for Caption Key ending with: {expectedCaptionSuffix}\n");

            // Find a key that ends with "_photoCaptionX"
            foreach (var key in webhookData.Keys)
            {
                if (key.EndsWith(expectedCaptionSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.AppendAllText(logFilePath, $"Found Matching Caption Key: {key}\n");
                    return key;
                }
            }

            System.IO.File.AppendAllText(logFilePath, $"No matching caption key found for {photoKey}\n");
            return null;
        }
    }


}
