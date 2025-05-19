using CityWatch.RadioCheck.Helpers;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Dropbox.Api.FileProperties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;
using DocumentFormat.OpenXml.Wordprocessing;
using CityWatch.RadioCheck.Models;

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
        private readonly IConfiguration _configuration;
        private string templateFileName;
        private string jsonMappingFile;
        private string jsonImageToFolderMappingFile;
        private string uploadFolder;
        //private const string JotFormApiKey = "6a5b7d0e94fdac941d2f857a5f096e47";
        //private const string JotFormApiKey = "4a0a2fe279684c4953af50311f5a2a93";
        public WebhookController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            templateFileName = "Template.xlsx";
            jsonMappingFile = "form_fields_mapping.json";
            jsonImageToFolderMappingFile = "image_folder_mapping.json";
            uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform");
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
                string submissionFolder = Path.Combine(uploadFolder, formName, workOrder);
                string templateFolder = Path.Combine(uploadFolder, formName);
                if (!Directory.Exists(submissionFolder))
                    Directory.CreateDirectory(submissionFolder);

                string logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
                string webhookFilePath = Path.Combine(submissionFolder, "webhook_test.txt");
                string excelFilePath = Path.Combine(submissionFolder, $"{formName}_{workOrder}_Output_data.xlsx");
                string jsonFilePath = Path.Combine(submissionFolder, "image_captions.json");

                await System.IO.File.AppendAllTextAsync(webhookFilePath, rawJson + Environment.NewLine);
                WriteLog(logFilePath, $"Webhook received. Data saved for Submission ID: {submissionID}");

                if (webhookData != null)
                {
                    await DownloadAllFiles(webhookData, submissionFolder, logFilePath);
                    // Save JSON to a file
                    string jsonOutput = GetImageNamesAndCaptionsJson(webhookData, logFilePath);
                    await System.IO.File.WriteAllTextAsync(jsonFilePath, jsonOutput);
                    // Compress image files
                    ImageZipper.CreateImageZip(submissionFolder, $"{submissionFolder}\\Compressed_Images", $"{workOrder}_images.zip");
                    if (DoesTemplateExists(templateFolder))
                    {
                        //CreateExcelInTemplateFormat(excelFilePath, webhookData);
                        UpdateTemplateUsingJsonMapping(templateFolder, excelFilePath, webhookData);
                        //insert images in the excel file.
                        CheckAndInsertImageInExcel(templateFolder, excelFilePath, workOrder, "image_captions.json");
                    }
                    else
                    {
                        AppendToExcel(excelFilePath, webhookData);
                    }
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
                var JotFormApiKey = _configuration["jotformSettings:ApiKey"];
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


        private void UpdateTemplateUsingJsonMapping(string TemplateFolder, string excelFilePath, Dictionary<string, object> webhookData)
        {
            // Load field mappings: ExcelHeader -> WebhookDataKey
            string templateFileWithPath = Path.Combine(TemplateFolder, templateFileName);
            string jsonMappingFileWithPath = Path.Combine(TemplateFolder, jsonMappingFile);

            var mappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
            var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);

            //Create a copy of template file in the new folder for export
            System.IO.File.Copy(templateFileWithPath, excelFilePath, true);


            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet("OutputData");

                int headerRow = 3;
                int dataRow = 4;
                int col = 1;

                // Traverse headers in row 3
                while (!string.IsNullOrEmpty(worksheet.Cell(headerRow, col).GetString()))
                {
                    string excelHeader = worksheet.Cell(headerRow, col).GetString();

                    // Find webhook key where value in the mapping matches Excel header
                    var matchingMapping = fieldMappings.FirstOrDefault(kvp => kvp.Value == excelHeader);
                    if (!string.IsNullOrEmpty(matchingMapping.Key) && webhookData.TryGetValue(matchingMapping.Key, out var rawValue))
                    {
                        object cellValue = null;

                        if (rawValue is JObject dateObj &&
                            dateObj["day"] != null && dateObj["month"] != null && dateObj["year"] != null &&
                            int.TryParse(dateObj["day"]?.ToString(), out int day) &&
                            int.TryParse(dateObj["month"]?.ToString(), out int month) &&
                            int.TryParse(dateObj["year"]?.ToString(), out int year))
                        {
                            // Format date to dd/MM/yyyy or as DateTime
                            DateTime date = new DateTime(year, month, day);
                            cellValue = date.ToString("dd/MM/yyyy");
                        }
                        else if (rawValue != null && !string.IsNullOrWhiteSpace(rawValue.ToString()))
                        {
                            cellValue = rawValue.ToString();
                        }

                        // Write to Excel only if there's a value
                        if (cellValue != null)
                        {
                            worksheet.Cell(dataRow, col).Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }

                    col++;
                }

                workbook.Save();
            }
        }


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

        private bool DoesTemplateExists(string TemplateFolder)
        {
            // Check for Template.xlsx specifically
            string templatePath = Path.Combine(TemplateFolder, templateFileName);
            string jsonMappingPath = Path.Combine(TemplateFolder, jsonMappingFile);
            if (!System.IO.File.Exists(templatePath) && !System.IO.File.Exists(jsonMappingPath))
            {
                return false;
            }
            return true;
        }

        private void CheckAndInsertImageInExcel(string TemplateFolder, string excelFilePath, string workOrderId,string image_captions_List_jsonFileName)
        {
            string jsonImageMappingFileWithPath = Path.Combine(TemplateFolder, jsonImageToFolderMappingFile);
            if (!System.IO.File.Exists(jsonImageMappingFileWithPath))
                return;

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet("Result");
                var mappingJson = System.IO.File.ReadAllText(jsonImageMappingFileWithPath);
                var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);
                string startColumnLetter = "C";
                string endColumnLetter = "AC";
                foreach (var kvp in fieldMappings)
                {
                    string _Headingkey = kvp.Key;
                    string _Foldervalue = kvp.Value;

                    // write the heading to the cell (heading) and border it
                    int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    int headingRow = lastUsedRow + 1;

                    // Get starting and ending column numbers from letters
                    int startCol = XLHelper.GetColumnNumberFromLetter(startColumnLetter);
                    int endCol = XLHelper.GetColumnNumberFromLetter(endColumnLetter);

                    //worksheet.Cell(headingRow, 3).Value = _Headingkey;
                    var headerCellRange = worksheet.Range(headingRow, startCol, headingRow, endCol);
                    headerCellRange.Merge().Value = _Headingkey;
                    // Apply borders on all sides
                    headerCellRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    headerCellRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    headerCellRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    headerCellRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    headerCellRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    headerCellRange.Style.Font.Bold = true;

                    // read json file for image and caption from the folder in value
                    string _folderToSearchImage = Path.Combine(uploadFolder, _Foldervalue, workOrderId);
                    string _file_image_caption = Path.Combine(_folderToSearchImage, image_captions_List_jsonFileName);
                    if (System.IO.File.Exists(_file_image_caption))
                    {
                        try
                        {
                            string jsonData = System.IO.File.ReadAllText(_file_image_caption);
                            List<ImageCaptionModel> captionsList = JsonConvert.DeserializeObject<List<ImageCaptionModel>>(jsonData);
                            if(captionsList != null  && captionsList.Count > 0)
                            {
                                foreach(var f in  captionsList)
                                {
                                    Image img = null;
                                    string _imageFileToread = Path.Combine(_folderToSearchImage, f.ImageName);
                                    if (System.IO.File.Exists(_imageFileToread))
                                    {
                                        img = Image.FromFile(_imageFileToread);
                                    }                                       

                                    InsertImageWithCaption(ref worksheet, f.Caption, img, 670, startColumnLetter, endColumnLetter);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading JSON file {_file_image_caption}.  Error: {ex.Message}");
                        }
                    }
                }


                workbook.Save();
            }

                
        }

        private static void InsertImageWithCaption(ref IXLWorksheet worksheet, string caption, Image image, int maxImageWidth, string startColumnLetter, string endColumnLetter)
        {
            if (worksheet == null)
                throw new ArgumentNullException(nameof(worksheet));
                        
            //var worksheet = workbook.Worksheet(worksheetName);

            // Find the last used row (or 1 if empty)
            int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int imageRow = lastUsedRow + 1;
            int captionRow = imageRow + 1;

            // Get starting and ending column numbers from letters
            int startCol = XLHelper.GetColumnNumberFromLetter(startColumnLetter);
            int endCol = XLHelper.GetColumnNumberFromLetter(endColumnLetter);

            // Merge the range for the image
            var imageCellRange = worksheet.Range(imageRow, startCol, imageRow, endCol);
            imageCellRange.Merge();
            imageCellRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            imageCellRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            imageCellRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            imageCellRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            //imageCellRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            imageCellRange.Style.Font.Bold = true;

            if (image != null)
            {
                // Resize image if it exceeds max width
                if (image.Width > maxImageWidth)
                {
                    float scale = (float)maxImageWidth / image.Width;
                    int newWidth = maxImageWidth;
                    int newHeight = (int)(image.Height * scale);

                    var resized = new Bitmap(newWidth, newHeight);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(image, 0, 0, newWidth, newHeight);
                    }
                    image.Dispose();
                    image = resized;
                }

                // Save to memory stream
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png); // PNG for better quality
                    ms.Seek(0, SeekOrigin.Begin);

                    // Adjust row height to fit image (approximate conversion)
                    float rowHeight = image.Height * 0.78f;
                    worksheet.Row(imageRow).Height = rowHeight;

                    // Add picture to sheet 
                    var picture = worksheet.AddPicture(ms)
                                           .MoveTo(worksheet.Cell(imageRow, startCol));                                           
                                           //.WithPlacement(XLPicturePlacement.FreeFloating);                    
                }
            }
            else
            {                
                var noimageCellRange = worksheet.Range(imageRow, startCol, imageRow, endCol);
                noimageCellRange.Merge().Value = "No image";
                noimageCellRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Write caption in the row below image, merging the same column range
            var captionCellRange = worksheet.Range(captionRow, startCol, captionRow, endCol);
            captionCellRange.Merge().Value = caption;
            // Enable text wrapping
            captionCellRange.Style.Alignment.WrapText = true;
            // Auto-adjust row height to fit content
            worksheet.Row(captionRow).AdjustToContents();
            captionCellRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            captionCellRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            captionCellRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            captionCellRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            captionCellRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            
        }
    }


}
