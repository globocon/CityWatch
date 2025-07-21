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
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Spreadsheet;
using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace CityWatch.RadioCheck.API
{

    [Route("api/flashbuttwelding")]
    [ApiController]
    public class FlashButtWeldingWebhookController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string templateFileName;
        private string dailyWeldingReport_jsonMappingFile;
        private string dailyWeldReturn_jsonMappingFile;
        private string dailyInspect_jsonMappingFile;
        private string railHeatNumberRecord_jsonMappingFile;
        private string register_jsonMappingFile;
        private string uploadFolder;
        private string logFilePath;
        private string _excelfileendname;
        public FlashButtWeldingWebhookController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            templateFileName = "Template.xlsx";
            _excelfileendname = "LWRReport.xlsx";
            dailyWeldingReport_jsonMappingFile = "daily_welding_report_fields_mapping.json";
            dailyWeldReturn_jsonMappingFile = "daily_weld_return_fields_mapping.json";
            dailyInspect_jsonMappingFile = "daily_inspect_fields_mapping.json";
            railHeatNumberRecord_jsonMappingFile = "rail_heat_number_record_fields_mapping.json";
            register_jsonMappingFile = "register_path_mapping.json";
            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "Flashbutt", "webhook_log.txt"); ;
            uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "Flashbutt");
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



        [HttpPost("formsubmit")]
        public async Task<IActionResult> ReceiveFlashbuttWeldingWebhook()
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

                string DateWiseFolder = "UnknownDate";
                // Assuming webhookData is Dictionary<string, object>
                var key = webhookData.Keys.FirstOrDefault(k => k.EndsWith("Daily_weld_Checkshee_date"));
                if (key != null && webhookData.TryGetValue(key, out object dateObj) && dateObj is JObject jObj)
                {
                    int day = int.Parse((string)jObj["day"]);
                    int month = int.Parse((string)jObj["month"]);
                    int year = int.Parse((string)jObj["year"]);

                    DateTime date = new DateTime(year, month, day);
                    DateWiseFolder = date.ToString("ddMMyyyy");
                    // Console.WriteLine(DateWiseFolder);
                }

                string supervisor = webhookData != null ? webhookData.FirstOrDefault(kvp => kvp.Key.Contains("_supervisor")).Value?.ToString() ?? "UnknownSupervisor" : "UnknownSupervisor";
                string submissionFolder = Path.Combine(uploadFolder, DateWiseFolder, supervisor);
                //string templateFolder = uploadFolder;
                if (!Directory.Exists(submissionFolder))
                    Directory.CreateDirectory(submissionFolder);

                logFilePath = Path.Combine(submissionFolder, "webhook_log.txt");
                string webhookFilePath = Path.Combine(submissionFolder, "webhook_data.txt");
                string excelFilePath = Path.Combine(submissionFolder, $"{DateWiseFolder}_{supervisor}_{_excelfileendname}");

                await System.IO.File.AppendAllTextAsync(webhookFilePath, Environment.NewLine + rawJson);
                WriteLog($"Webhook received. Data saved for Submission ID: {submissionID}");

                CopyTemplateToFolder(uploadFolder, excelFilePath);
                await CreateExcelReportFile(excelFilePath, uploadFolder, webhookData);


                WriteLog($"Exiting From webhook.\n######################################################################\n\n\n");

                return Ok(new { message = $"Webhook received. Files saved in uploads/jotform/Flashbutt/{DateWiseFolder}/{supervisor}/" });
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("exceldatadownload")]
        public async Task<IActionResult> DownloadExcelFile([FromForm] string folder_Name, [FromForm] string supervisor_Name)
        {

            string submissionFolder = Path.Combine(uploadFolder, folder_Name, supervisor_Name);
            string fileName = $"{folder_Name}_{supervisor_Name}_{_excelfileendname}";
            string excelFilePath = Path.Combine(submissionFolder, fileName);

            // ## This is for Testing 
            string jsonDataFileWithPath = Path.Combine(submissionFolder, "webhook_data.txt");
            string rawJson = System.IO.File.ReadAllText(jsonDataFileWithPath);
            var rawArray = rawJson.Split(Environment.NewLine);
            rawJson = rawArray[rawArray.Length-1];
            var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;
            CopyTemplateToFolder(uploadFolder, excelFilePath);
            await CreateExcelReportFile(excelFilePath, uploadFolder, webhookData);
            // ## This is for Testing



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

        private async Task CreateExcelReportFile(string _excelReportFileName, string JsonMappingFileFolder, Dictionary<string, object> webhookData)
        {
            if (!System.IO.File.Exists(_excelReportFileName))
            {
                string log = $"Error: File {_excelReportFileName} not found for generating report.\n";
                log += $"Exiting From webhook.\n######################################################################\n\n\n";
                throw new Exception(message: log, innerException: null);
            }

            using (var workbook = new XLWorkbook(_excelReportFileName))
            {
                try
                {
                    string jsonMappingFileWithPath = "";
                    //Daily_Welding_Report_Data
                    var worksheet = workbook.Worksheet("Daily_Welding_Report_Data");
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, dailyWeldingReport_jsonMappingFile);
                    Update_Daily_Welding_Report_Data_in_Template(ref worksheet, jsonMappingFileWithPath, webhookData);

                    //Daily_Weld_Return_Data
                    worksheet = workbook.Worksheet("Daily_Weld_Return_Data");
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, dailyWeldReturn_jsonMappingFile);
                    Update_Daily_Weld_Return_Data_in_Template(ref worksheet, jsonMappingFileWithPath, webhookData);

                    //Daily_Inspect_Data
                    worksheet = workbook.Worksheet("Daily_Inspect_Data");
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, dailyInspect_jsonMappingFile);
                    Update_Daily_Inspect_Data_in_Template(ref worksheet, jsonMappingFileWithPath, webhookData);

                    //Rail_Heat_Number_Record_Data
                    worksheet = workbook.Worksheet("Rail_Heat_Number_Record_Data");
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, railHeatNumberRecord_jsonMappingFile);
                    Update_Rail_Heat_Number_Record_Data_in_Template(ref worksheet, jsonMappingFileWithPath, webhookData);

                    //Registers
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, register_jsonMappingFile);                    
                    var registerMappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
                    var registerPathMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(registerMappingJson);
                    foreach (var keyValue in registerPathMappings)
                    {
                        string ws_name = keyValue.Key;
                        if (workbook.TryGetWorksheet(ws_name, out worksheet))
                        {
                            if (worksheet != null)
                            {
                                Copy_Register_Data_To_Template(ref worksheet, keyValue.Value);
                            }
                        }
                    }



                }
                catch (Exception ex)
                {

                    string log = $"Error: Unable to create excel report file: {ex.Message}\n";
                    log += $"Exiting From webhook.\n######################################################################\n\n\n";
                    throw new Exception(message: log, innerException: ex.InnerException);
                }
                finally
                {
                    workbook.CalculationOnSave = true;
                    workbook.Save();
                    workbook.Dispose();
                }
            }
        }

        private void Update_Daily_Welding_Report_Data_in_Template(ref IXLWorksheet worksheet, string jsonMappingFileWithPath, Dictionary<string, object> webhookData)
        {
            // Load field mappings: ExcelHeader -> WebhookDataKey 
            var mappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
            var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);


            //int row = 1;   
            int headerCol = 1;
            int dataCol = 2;
            int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Traverse headers in col 1
            for (int row = 1; row <= lastUsedRow; row++)
            {
                string excelHeader = worksheet.Cell(row, headerCol).GetString();
                // Find webhook key where value in the mapping matches Excel header
                var matchingMapping = fieldMappings.FirstOrDefault(kvp => kvp.Key == excelHeader);

                //Check if Excel header is a table
                if (excelHeader != null && excelHeader.StartsWith("#TABLE_"))
                {
                    //List<ExcelTableClass> xlTblClassList = new List<ExcelTableClass>();
                    //for (int _currentRow = row + 1; _currentRow <= lastUsedRow; _currentRow++)
                    //{
                    //    string excelColHeader = worksheet.Cell(_currentRow, headerCol).GetString();
                    //    if (excelColHeader != null && excelColHeader.Contains("#_TABLE_"))
                    //    {
                    //        xlTblClassList.Add(new ExcelTableClass() { _xlRowNum = _currentRow, _xlColName = excelColHeader });
                    //    }
                    //}

                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
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
                                worksheet.Cell(row, column).Value = value;
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
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
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
                            worksheet.Cell(row, dataCol).Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }
                }


            }
        }

        private void Update_Daily_Weld_Return_Data_in_Template(ref IXLWorksheet worksheet, string jsonMappingFileWithPath, Dictionary<string, object> webhookData)
        {
            // Load field mappings: ExcelHeader -> WebhookDataKey 
            var mappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
            var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);


            //int row = 1;   
            int headerCol = 1;
            int dataCol = 2;
            int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Traverse headers in col 1
            for (int row = 1; row <= lastUsedRow; row++)
            {
                string excelHeader = worksheet.Cell(row, headerCol).GetString();
                // Find webhook key where value in the mapping matches Excel header
                var matchingMapping = fieldMappings.FirstOrDefault(kvp => kvp.Key == excelHeader);

                //Check if Excel header is a table
                if (excelHeader != null && excelHeader.StartsWith("#TABLE_"))
                {
                    //List<ExcelTableClass> xlTblClassList = new List<ExcelTableClass>();
                    //for (int _currentRow = row + 1; _currentRow <= lastUsedRow; _currentRow++)
                    //{
                    //    string excelColHeader = worksheet.Cell(_currentRow, headerCol).GetString();
                    //    if (excelColHeader != null && excelColHeader.Contains("#_TABLE_"))
                    //    {
                    //        xlTblClassList.Add(new ExcelTableClass() { _xlRowNum = _currentRow, _xlColName = excelColHeader });
                    //    }
                    //}

                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
                    {
                        row++;
                        int start_row = row;
                        int current_row = row;
                        if (!string.IsNullOrWhiteSpace(rawValue?.ToString()))
                        {
                            var data = JObject.Parse(rawValue.ToString());
                            int column = 2; // Start from column B
                            foreach (var item in data.Properties().Where(p => p.Name.All(char.IsDigit)))
                            {
                                var rowValues = (JObject)item.Value;
                                for (int i = 0; i < data.Count - 2; i++)
                                {
                                    string value = rowValues[i.ToString()]?.ToString() ?? ""; // null-safe
                                    worksheet.Cell(row, column).Value = value;
                                    row++;
                                }
                                current_row = rowValues.Count;
                                column++;
                                row = start_row;
                            }
                            row = current_row + start_row - 2;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
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
                            worksheet.Cell(row, dataCol).Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }
                }


            }
        }

        private void Update_Daily_Inspect_Data_in_Template(ref IXLWorksheet worksheet, string jsonMappingFileWithPath, Dictionary<string, object> webhookData)
        {
            // Load field mappings: ExcelHeader -> WebhookDataKey 
            var mappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
            var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);


            //int row = 1;   
            int headerCol = 1;
            int dataCol = 2;
            int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Traverse headers in col 1
            for (int row = 1; row <= lastUsedRow; row++)
            {
                string excelHeader = worksheet.Cell(row, headerCol).GetString();
                // Find webhook key where value in the mapping matches Excel header
                var matchingMapping = fieldMappings.FirstOrDefault(kvp => kvp.Key == excelHeader);

                //Check if Excel header is a table
                if (excelHeader != null && excelHeader.StartsWith("#TABLE_"))
                {
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
                    {
                        row++;
                        if (!string.IsNullOrWhiteSpace(rawValue?.ToString()))
                        {
                            var data = JObject.Parse(rawValue.ToString());
                            int column = 2; // Start from column B
                            foreach (var item in data.Properties().Where(p => p.Name.All(char.IsDigit)))
                            {
                                column = 2;
                                var rowValues = (JObject)item.Value;
                                for (int i = 0; i < rowValues.Count; i++)
                                {
                                    string value = rowValues[i.ToString()]?.ToString() ?? ""; // null-safe
                                    worksheet.Cell(row, column).Value = value;
                                    column++;
                                }
                                row++;
                            }
                            row--;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
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
                            worksheet.Cell(row, dataCol).Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }
                }


            }
        }

        private void Update_Rail_Heat_Number_Record_Data_in_Template(ref IXLWorksheet worksheet, string jsonMappingFileWithPath, Dictionary<string, object> webhookData)
        {
            // Load field mappings: ExcelHeader -> WebhookDataKey  
            var mappingJson = System.IO.File.ReadAllText(jsonMappingFileWithPath);
            var fieldMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);


            //int row = 1;   
            int headerCol = 1;
            int dataCol = 2;
            int lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Traverse headers in col 1
            for (int row = 1; row <= lastUsedRow; row++)
            {
                string excelHeader = worksheet.Cell(row, headerCol).GetString();
                // Find webhook key where value in the mapping matches Excel header
                var matchingMapping = fieldMappings.FirstOrDefault(kvp => kvp.Key == excelHeader);

                //Check if Excel header is a table
                if (excelHeader != null && excelHeader.StartsWith("#TABLE_"))
                {
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
                    {
                        row++;
                        if (!string.IsNullOrWhiteSpace(rawValue?.ToString()))
                        {
                            var data = JObject.Parse(rawValue.ToString());
                            int column = 2; // Start from column B
                            foreach (var item in data.Properties().Where(p => p.Name.All(char.IsDigit)))
                            {
                                column = 2;
                                var rowValues = (JObject)item.Value;
                                for (int i = 0; i < rowValues.Count; i++)
                                {
                                    string value = rowValues[i.ToString()]?.ToString() ?? ""; // null-safe
                                    worksheet.Cell(row, column).Value = value;
                                    column++;
                                }
                                row++;
                            }
                            row--;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(matchingMapping.Value) && webhookData.TryGetValue(matchingMapping.Value, out var rawValue))
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
                            worksheet.Cell(row, dataCol).Value = cellValue is DateTime dt ? dt : cellValue.ToString();
                        }
                    }
                }


            }
        }

        private void Copy_Register_Data_To_Template(ref IXLWorksheet worksheet, string sourceFile)
        {
            string _sourceFileName = Path.Combine(Directory.GetCurrentDirectory(), sourceFile);
            if (!System.IO.File.Exists(_sourceFileName))
            {
                WriteLog($"Source file {_sourceFileName} not found.");
                return;
            }

            // Open the workbook
            using (var sourceworkbook = new XLWorkbook(_sourceFileName))
            {
                var sourceSheet = sourceworkbook.Worksheet(1);

                // Get the range of used cells (excluding the first row which is header)
                var usedRange = sourceSheet.RangeUsed();

                if (usedRange != null && usedRange.RowCount() > 1)
                {
                    // Exclude header (start from second row)
                    var dataRange = sourceSheet.Range(
                        usedRange.FirstCell().Address.RowNumber + 1,  // from row 2
                        usedRange.FirstCell().Address.ColumnNumber,
                        usedRange.LastCell().Address.RowNumber,
                        usedRange.LastCell().Address.ColumnNumber
                    );
                                        
                    // Paste into target sheet starting at cell A2
                    dataRange.CopyTo(worksheet.Cell(4, 1));
                }                
            }
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
    }

    //public class ExcelTableClass
    //{
    //    public int _xlRowNum { get; set; }
    //    public string _xlColName { get; set; }
    //}

}
