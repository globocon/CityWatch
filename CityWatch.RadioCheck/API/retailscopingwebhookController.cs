using CityWatch.RadioCheck.Helpers;
using CityWatch.RadioCheck.Models;
using CityWatch.RadioCheck.Services;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.API
{

    [Route("api/retailscopingwebhook")]
    [ApiController]
    public class retailscopingwebhookController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IJotFormService _jotFormService;
        private string templateFileName;
        private string download_jsonMappingFile;
        private string deliveries_DataFile;
        private string execution_DataFile;
        private string jsonImageToFolderMappingFile;
        private string uploadFolder;
        private string logFilePath;
        private string compressed_image_folder_name;
        private string webhookFilePath;
        private string excelFilePath;
        private string jsonFilePath;
        private string templateFolder;
        private string workOrder;
        private string submissionFolder;
        private string formName;


        public retailscopingwebhookController(IJotFormService jotFormService)
        {
            _httpClient = new HttpClient();
            _jotFormService = jotFormService;
            templateFileName = "Fortescue_Rerail_Scoping_Desktop.xlsx";
            download_jsonMappingFile = "download_form_fields_mapping.json";
            jsonImageToFolderMappingFile = "image_folder_mapping.json";
            deliveries_DataFile = "Delivery Data.xlsx";
            execution_DataFile = "Execution Data.xlsx";
            compressed_image_folder_name = "Compressed_Images";
            logFilePath = "";
            webhookFilePath = "";
            excelFilePath = "";
            jsonFilePath = "";
            templateFolder = "";
            workOrder = "";
            submissionFolder = "";
            formName = "";
            uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform");
        }

        [HttpGet("formsubmit")]
        public async Task<IActionResult> ReceiveTest()
        {
            return Ok(new { message = $"Api call success..." });
        }

        [HttpPost("formsubmit")]
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


                formName = await GetFormNameFromJotForm(formID);
                //string workOrder = webhookData != null && webhookData.ContainsKey("q3_workOrder") ? webhookData["q3_workOrder"].ToString() : "UnknownWorkOrder";
                workOrder = webhookData != null ? webhookData.FirstOrDefault(kvp => kvp.Key.ToLower().Contains("_workorder")).Value?.ToString() ?? "UnknownWorkOrder" : "UnknownWorkOrder";
                submissionFolder = Path.Combine(uploadFolder, formName, workOrder);
                templateFolder = Path.Combine(uploadFolder, formName);
                if (!Directory.Exists(submissionFolder))
                    Directory.CreateDirectory(submissionFolder);

                logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
                webhookFilePath = Path.Combine(submissionFolder, "webhook_data.txt");
                excelFilePath = Path.Combine(submissionFolder, $"{formName}_{workOrder}_Output_data.xlsx");
                jsonFilePath = Path.Combine(submissionFolder, "image_captions.json");

                await System.IO.File.AppendAllTextAsync(webhookFilePath, Environment.NewLine + rawJson);
                WriteLog($"Webhook received. Data saved for Submission ID: {submissionID}");

                CopyTemplateToFolder(templateFolder, excelFilePath);
                await CreateExcelReportFile(webhookData);


                WriteLog($"Exiting From webhook.\n######################################################################\n\n\n");

                return Ok(new { message = $"Webhook received. Files saved in uploads/jotform/{formName}/{workOrder}/" });

            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "webhook_log.txt");
                WriteLog($"Error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("exceldatadownload")]
        public async Task<IActionResult> DownloadExcelFile([FromForm] string formName, [FromForm] string workOrder)
        {

            submissionFolder = Path.Combine(uploadFolder, formName, workOrder);
            templateFolder = Path.Combine(uploadFolder, formName);
            string fileName = $"{formName}_{workOrder}_Output_data.xlsx";
            excelFilePath = Path.Combine(submissionFolder, fileName);
            logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
            jsonFilePath = Path.Combine(submissionFolder, "image_captions.json");


            if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // ## This is for Testing 
                string jsonDataFileWithPath = Path.Combine(submissionFolder, "webhook_data.txt");
                string rawJson = System.IO.File.ReadAllText(jsonDataFileWithPath);
                var rawArray = rawJson.Split(Environment.NewLine);
                rawJson = rawArray[rawArray.Length - 1];
                var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;
                CopyTemplateToFolder(templateFolder, excelFilePath);
                await CreateExcelReportFile(webhookData);
                // ## This is for Testing
            }

            // 🔸 Return file as response
            if (!System.IO.File.Exists(excelFilePath))
            {
                return BadRequest("File does not Exists !!!");
            }
            else
            {
                var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        [HttpPost("exceldatafileupload")]
        public async Task<IActionResult> UploadExcelFile(IFormFile file, [FromForm] string fileType, [FromForm] string formName)
        {
            var status = true;
            var message = "File uploaded successfully !!!";
            string filenameToUpload;
            var allowedExtensions = new[] { ".xls", ".xlsx" };

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid file type. Only Excel files are allowed.");

            if (string.IsNullOrEmpty(formName))
                return BadRequest("Invalid form name.");


            if (!string.IsNullOrEmpty(fileType))
            {
                if (fileType.Equals("DeliveriesExcel"))
                {
                    filenameToUpload = deliveries_DataFile;
                }
                else if (fileType.Equals("ExecutionExcel"))
                {
                    filenameToUpload = execution_DataFile;
                }
                else
                {
                    return BadRequest("Invalid upload file type. Only predefined Excel files are allowed.");
                }
            }
            else
            {
                return BadRequest("Invalid upload file type. Only predefined Excel files are allowed.");
            }

            string uploadsPath = Path.Combine(uploadFolder, formName);

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, filenameToUpload);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //return Ok("File uploaded successfully.");
            return Ok(new { status = status, message = message });
        }

        private async Task CreateExcelReportFile(Dictionary<string, object> webhookData)
        {
            string JsonMappingFileFolder = uploadFolder;
            if (!System.IO.File.Exists(excelFilePath))
            {
                string log = $"Error: File {excelFilePath} not found for generating report.\n";
                log += $"Exiting From webhook.\n######################################################################\n\n\n";
                throw new Exception(message: log, innerException: null);
            }


            if (webhookData != null)
            {
                await DownloadAllFiles(webhookData, submissionFolder);
                // Save JSON to a file
                string jsonOutput = GetImageNamesAndCaptionsJson(webhookData);
                await System.IO.File.WriteAllTextAsync(jsonFilePath, jsonOutput);
                // Compress image files                   
                ImageZipper.CreateThumbnail(submissionFolder, $"{submissionFolder}\\{compressed_image_folder_name}");                
            }


            FileInfo fileinfo = new FileInfo(excelFilePath);
            //ExcelPackage workbook = new ExcelPackage(fileinfo);                   

            using (var workbook = new ExcelPackage(fileinfo))
            {
                try
                {                    
                    //Main_Scope_Data
                    var worksheet = workbook.Workbook.Worksheets["Main_Scope_Data"];
                    Update_Data_in_Template(ref worksheet, webhookData);

                    //Resources_Equipment_Data
                    worksheet = workbook.Workbook.Worksheets["Resources_Equipment_Data"];
                    Update_Data_in_Template(ref worksheet, webhookData);

                    //Site_Photos_Data
                    worksheet = workbook.Workbook.Worksheets["Site_Photos_Data"];
                    var imagedestworksheet = workbook.Workbook.Worksheets["Site Photos"];
                    Update_Image_in_Template(ref worksheet, ref imagedestworksheet, webhookData);

                    //Images_Data
                    worksheet = workbook.Workbook.Worksheets["Images_Data"];
                    Update_Image_in_Template(ref worksheet, webhookData);

                }
                catch (Exception ex)
                {

                    string log = $"Error: Unable to create excel report file: {ex.Message}\n";
                    log += $"Exiting From webhook.\n######################################################################\n\n\n";
                    throw new Exception(message: log, innerException: ex.InnerException);
                }
                finally
                {
                    //workbook.CalculationOnSave = true;
                    workbook.Save();
                    workbook.Dispose();
                }
            }
        }

        private void Update_Data_in_Template(ref ExcelWorksheet worksheet, Dictionary<string, object> webhookData)
        {
            int headerCol = 1;
            int dataCol = 3;
            int lastUsedRow = worksheet.Dimension.End.Row == 0 ? 1 : worksheet.Dimension.End.Row;

            // Traverse headers in col 1
            for (int row = 2; row <= lastUsedRow; row++)
            {
                string excelHeader = Convert.ToString(worksheet.GetValue(row, headerCol));
                string jsonKeyInWebhook = Convert.ToString(worksheet.GetValue(row, headerCol + 1));



                if (string.IsNullOrEmpty(jsonKeyInWebhook) || string.IsNullOrEmpty(excelHeader))
                    continue;

                //Check if Excel header is a table
                if (excelHeader != null && excelHeader.StartsWith("#TABLE_"))
                {
                    if (!string.IsNullOrEmpty(jsonKeyInWebhook) && webhookData.TryGetValue(jsonKeyInWebhook, out var rawValue))
                    {
                        row++;
                        int start_row = row;
                        int current_row = row;
                        var data = JObject.Parse(rawValue.ToString());
                        int column = 2; // Start from column B
                        foreach (var item in data.Properties().Where(p => p.Name.All(char.IsDigit)))
                        {
                            var rowValues = (JObject)item.Value;
                            for (int i = 0; i < data.Count - 2; i++)
                            {
                                string value = rowValues[i.ToString()]?.ToString() ?? ""; // null-safe
                                worksheet.Cells[row, column].Value = value;
                                current_row = row;
                                row++;
                            }
                            column++;
                            row = start_row;
                        }
                        row = current_row - 1;
                    }

                }
                else
                {
                    if (!string.IsNullOrEmpty(jsonKeyInWebhook) && webhookData.TryGetValue(jsonKeyInWebhook, out var rawValue))
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
                            worksheet.Cells[row, dataCol].Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }
                }
            }
        }

        private void Update_Image_in_Template(ref ExcelWorksheet worksheet, ref ExcelWorksheet DestWorkSheet, Dictionary<string, object> webhookData)
        {
            int headerCol = 1;
            int lastUsedRow = worksheet.Dimension.End.Row == 0 ? 1 : worksheet.Dimension.End.Row;

            // Traverse headers in col 1
            for (int row = 2; row <= lastUsedRow; row++)
            {
                string excelHeader = Convert.ToString(worksheet.GetValue(row, headerCol));
                string jsonKeyInWebhook = Convert.ToString(worksheet.GetValue(row, headerCol + 1));

                string destrowstr = Convert.ToString(worksheet.GetValue(row, headerCol + 2));
                string destcolstr = Convert.ToString(worksheet.GetValue(row, headerCol + 3));



                if (string.IsNullOrEmpty(jsonKeyInWebhook) || string.IsNullOrEmpty(excelHeader) || string.IsNullOrEmpty(destrowstr) || string.IsNullOrEmpty(destcolstr))
                    continue;

                int.TryParse(destrowstr, out int destrow);
                int.TryParse(destcolstr, out int destcol);


                if (webhookData.TryGetValue(jsonKeyInWebhook, out var rawValue))
                {
                    object cellValue = null;

                    if (rawValue != null && !string.IsNullOrWhiteSpace(rawValue.ToString()))
                    {

                        if (rawValue is JArray fileArray)
                        {
                            var fileUrls = fileArray.ToObject<List<string>>();
                            foreach (var fileUrl in fileUrls)
                            {
                                string normalizedUrl = Regex.Unescape(fileUrl).Replace("\\", "/").Trim();  // Unescape JSON & fix slashes

                                if (Uri.IsWellFormedUriString(normalizedUrl, UriKind.Absolute))
                                {
                                    // Get the filename from the URL
                                    string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                                    string filePath = Path.Combine($"{submissionFolder}\\{compressed_image_folder_name}", fileName);

                                    if (!System.IO.File.Exists(filePath))
                                    {
                                        WriteLog($"File not found: {filePath}");
                                        continue; // Skip if file does not exist
                                    }
                                    // Load the image from file
                                    using (Image image = Image.FromFile(filePath))
                                    {
                                        var picture = DestWorkSheet.Drawings.AddPicture($"Image_{excelHeader}", image);

                                        // Set image position to top-left corner of cell
                                        picture.SetPosition(destrow - 1, 5, destcol - 1, 5);  // (rowIdx, rowOffsetPx, colIdx, colOffsetPx)



                                        // Get cell size in pixels
                                        double columnWidth = DestWorkSheet.Column(destcol).Width;
                                        double rowHeight = DestWorkSheet.Row(destrow).Height;

                                        int cellWidthPx = ExcelColumnWidthToPixels(columnWidth);
                                        int cellHeightPx = ExcelRowHeightToPixels(rowHeight) * 7;

                                        // Set image size to match cell
                                        picture.SetSize(cellWidthPx - 6, cellHeightPx - 6);



                                        ////// Optional: Resize image to fit cell
                                        ////picture.SetSize(100); // scale percentage (100 = original size)

                                        ////var imageHeight = image.Height;
                                        ////var imageWidth = image.Width;


                                        ////// Set row height (e.g., row 4)
                                        ////float dpi = image.VerticalResolution; // usually 96
                                        ////double rowHeight = (image.Height / dpi) * 72;
                                        ////DestWorkSheet.Row(destrow).Height = rowHeight;

                                        ////// Set column width (e.g., column B)
                                        ////int imagePixelWidth = image.Width;
                                        ////double columnWidth = imagePixelWidth / 7.0;
                                        ////DestWorkSheet.Column(destcol).Width = columnWidth;

                                        ////// Optional: Or resize to cell size
                                        //////picture.SetSize((int)DestWorkSheet.Column(destcol).Width, (int)DestWorkSheet.Row(destrow).Height);
                                    }
                                }
                                else
                                {
                                    WriteLog($"Invalid URL: {fileUrl}");
                                }
                            }
                        }
                        else
                        {
                            cellValue = rawValue.ToString();
                        }

                    }                   
                }
            }
        }

        private void Update_Image_in_Template(ref ExcelWorksheet worksheet, Dictionary<string, object> webhookData)
        {
            int headerCol = 1;
            int lastUsedRow = worksheet.Dimension.End.Row == 0 ? 1 : worksheet.Dimension.End.Row;

            ExcelWorksheet DestWorkSheet;

            // Traverse headers in col 1
            for (int row = 2; row <= lastUsedRow; row++)
            {
                string excelSheet = Convert.ToString(worksheet.GetValue(row, headerCol));
                string jsonKeyInWebhook = Convert.ToString(worksheet.GetValue(row, headerCol + 1));

                string destrowstr = Convert.ToString(worksheet.GetValue(row, headerCol + 2));
                string destcolstr = Convert.ToString(worksheet.GetValue(row, headerCol + 3));


                if (string.IsNullOrEmpty(jsonKeyInWebhook) || string.IsNullOrEmpty(excelSheet) || string.IsNullOrEmpty(destrowstr) || string.IsNullOrEmpty(destcolstr))
                    continue;

                DestWorkSheet = worksheet.Workbook.Worksheets[excelSheet];
                int.TryParse(destrowstr, out int destrow);
                int.TryParse(destcolstr, out int destcol);


                if (webhookData.TryGetValue(jsonKeyInWebhook, out var rawValue))
                {
                    object cellValue = null;

                    if (rawValue != null && !string.IsNullOrWhiteSpace(rawValue.ToString()))
                    {

                        if (rawValue is JArray fileArray)
                        {
                            var fileUrls = fileArray.ToObject<List<string>>();
                            foreach (var fileUrl in fileUrls)
                            {
                                string normalizedUrl = Regex.Unescape(fileUrl).Replace("\\", "/").Trim();  // Unescape JSON & fix slashes

                                if (Uri.IsWellFormedUriString(normalizedUrl, UriKind.Absolute))
                                {
                                    // Get the filename from the URL
                                    string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                                    string filePath = Path.Combine($"{submissionFolder}\\{compressed_image_folder_name}", fileName);

                                    if(!System.IO.File.Exists(filePath))
                                    {
                                        WriteLog($"File not found: {filePath}");
                                        continue; // Skip if file does not exist
                                    }
                                    // Load the image from file
                                    using (Image image = Image.FromFile(filePath))
                                    {
                                        var picture = DestWorkSheet.Drawings.AddPicture($"Image_{excelSheet}", image);

                                        // Set image position to top-left corner of cell
                                        picture.SetPosition(destrow - 1, 5, destcol - 1, 5);  // (rowIdx, rowOffsetPx, colIdx, colOffsetPx)



                                        // Get cell size in pixels
                                        double columnWidth = DestWorkSheet.Column(destcol).Width;
                                        double rowHeight = DestWorkSheet.Row(destrow).Height;

                                        int cellWidthPx = ExcelColumnWidthToPixels(columnWidth);
                                        int cellHeightPx = ExcelRowHeightToPixels(rowHeight) * 60;

                                        // Set image size to match cell
                                        picture.SetSize(cellWidthPx - 6, cellHeightPx - 6);



                                        ////// Optional: Resize image to fit cell
                                        ////picture.SetSize(100); // scale percentage (100 = original size)

                                        ////var imageHeight = image.Height;
                                        ////var imageWidth = image.Width;


                                        ////// Set row height (e.g., row 4)
                                        ////float dpi = image.VerticalResolution; // usually 96
                                        ////double rowHeight = (image.Height / dpi) * 72;
                                        ////DestWorkSheet.Row(destrow).Height = rowHeight;

                                        ////// Set column width (e.g., column B)
                                        ////int imagePixelWidth = image.Width;
                                        ////double columnWidth = imagePixelWidth / 7.0;
                                        ////DestWorkSheet.Column(destcol).Width = columnWidth;

                                        ////// Optional: Or resize to cell size
                                        //////picture.SetSize((int)DestWorkSheet.Column(destcol).Width, (int)DestWorkSheet.Row(destrow).Height);
                                    }
                                }
                                else
                                {
                                    WriteLog($"Invalid URL: {fileUrl}");
                                }
                            }
                        }
                        else
                        {
                            cellValue = rawValue.ToString();
                        }

                    }
                }
            }
        }

        private int ExcelColumnWidthToPixels(double excelColumnWidth)
        {
            // Approximate formula for standard fonts (Calibri 11)
            return (int)Math.Round(excelColumnWidth * 7); // 1 Excel width ≈ 7 pixels
        }

        private int ExcelRowHeightToPixels(double excelRowHeight)
        {
            // 1 point = 1/72 inch, 1 pixel ≈ 0.75 point (at 96 DPI)
            return (int)Math.Round(excelRowHeight * 96 / 72);
        }

        private void CopyTemplateToFolder(string _sourceFolder, string _destinationFileName)
        {
            string _sourceFileName = Path.Combine(_sourceFolder, templateFileName);
            string _destinationFolder = Path.GetDirectoryName(_destinationFileName);
            if (!System.IO.File.Exists(_sourceFileName))
                return;

            if (!Directory.Exists(_destinationFolder))
                Directory.CreateDirectory(_destinationFolder);

            try
            {
                System.IO.File.Copy(_sourceFileName, _destinationFileName, true);
            }
            catch (Exception ex)
            {
                string log = $"Error: Unable to Copy Template file: {ex.Message}\n";
                log += $"Exiting From webhook.\n######################################################################\n\n\n";
                throw new Exception(message: log, innerException: ex.InnerException);
            }
        }

        private async Task<string> GetFormNameFromJotForm(string formID)
        {
            try
            {
                var formResponse = await _jotFormService.GetFormNameFromJotForm(formID);

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

        private async Task DownloadAllFiles(Dictionary<string, object> webhookData, string saveDirectory)
        {
            foreach (var entry in webhookData)
            {
                WriteLog($"Processing key: {entry.Key}");

                if (entry.Value is JArray fileArray) // Direct file list
                {
                    await ProcessFileArray(fileArray, saveDirectory);
                }
                else if (entry.Value is JObject nestedObject) // Nested JSON object
                {
                    await DownloadAllFiles(nestedObject.ToObject<Dictionary<string, object>>(), saveDirectory);
                }
                else if (entry.Value is Dictionary<string, object> nestedDict) // Nested Dictionary
                {
                    // First process direct file lists inside the nested dictionary
                    foreach (var subEntry in nestedDict)
                    {
                        if (subEntry.Value is JArray subFileArray)
                        {
                            await ProcessFileArray(subFileArray, saveDirectory);
                        }
                    }
                    // Then, recursively process the nested dictionary
                    await DownloadAllFiles(nestedDict, saveDirectory);
                }
            }
        }

        private async Task ProcessFileArray(JArray fileArray, string saveDirectory)
        {
            var fileUrls = fileArray.ToObject<List<string>>();
            foreach (var fileUrl in fileUrls)
            {
                string normalizedUrl = Regex.Unescape(fileUrl).Replace("\\", "/").Trim();  // Unescape JSON & fix slashes

                if (Uri.IsWellFormedUriString(normalizedUrl, UriKind.Absolute))
                {
                    await DownloadAndSaveFile(normalizedUrl, saveDirectory);
                }
                else
                {
                    WriteLog($"Invalid URL: {fileUrl}");
                }
            }
        }

        private async Task DownloadAndSaveFile(string fileUrl, string saveDirectory)
        {
            // Chat GPT Code
            try
            {
                using var httpClient = new HttpClient();

                // Send HTTP GET request and ensure the response is successful
                using var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Get the filename from the URL
                string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                string filePath = Path.Combine(saveDirectory, fileName);

                // Create the directory if it doesn't exist
                Directory.CreateDirectory(saveDirectory);

                // Stream the content directly to the file
                await using var inputStream = await response.Content.ReadAsStreamAsync();
                await using var outputStream = System.IO.File.Create(filePath);
                await inputStream.CopyToAsync(outputStream);

                WriteLog($"Image downloaded: {fileName} from {fileUrl}");
            }
            catch (Exception ex)
            {
                WriteLog($"Error downloading image from {fileUrl}: {ex.Message}");
            }
        }

        private void WriteLog(string logMessage)
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

        public string GetImageNamesAndCaptionsJson(Dictionary<string, object> webhookData)
        {
            var imagesWithCaptions = new List<object>();

            try
            {
                System.IO.File.AppendAllText(logFilePath, "Processing Webhook Data...\n");

                foreach (var kvp in webhookData)
                {
                    System.IO.File.AppendAllText(logFilePath, $"Processing key: {kvp.Key}\n");

                    if (kvp.Key.ToLower().Contains("sitephotos_") && kvp.Value is object value)
                    {
                        System.IO.File.AppendAllText(logFilePath, $"Processing Image key: {kvp.Key}\n");
                        if (value is JArray array) // If multiple images exist
                        {
                            foreach (var item in array)
                            {
                                string imageUrl = item.ToString();
                                string imageName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

                                // Find the matching caption key
                                string captionKey = FindMatchingCaptionKey(webhookData, kvp.Key);
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
                            string imageName = string.Empty;

                            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri uriResult) &&
                                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                            {
                                imageName = Path.GetFileName(uriResult.AbsolutePath);
                            }

                            if (string.IsNullOrEmpty(imageName))
                                continue;

                            // Find the matching caption key
                            string captionKey = FindMatchingCaptionKey(webhookData, kvp.Key);
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

        private string FindMatchingCaptionKey(Dictionary<string, object> webhookData, string photoKey)
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
