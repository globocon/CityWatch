using CityWatch.RadioCheck.Models.JotForm;
using CityWatch.RadioCheck.Services;
using CityWatch.Web.Helpers;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.API
{

    [Route("api/flashbuttwelding")]
    [ApiController]
    public class FlashButtWeldingWebhookController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IJotFormService _jotFormService;
        private string templateFileName;
        private string dailyWeldingReport_jsonMappingFile;
        private string dailyWeldReturn_jsonMappingFile;
        private string dailyInspect_jsonMappingFile;
        private string railHeatNumberRecord_jsonMappingFile;
        private string register_jsonMappingFile;
        private string uploadFolder;
        private string logFileName;
        private string logFilePath;
        private string webhookoutputdataFilename;
        private string _excelfileendname;

        private string _WeldReturnRegister_XlSheetName;
        private string _ComplianceRegister_XlSheetName;
        private string _StringsRegister_XlSheetName;
        private string _WeldReturnRegister_FormID;
        private string _ComplianceRegister_FormID;
        private string _StringsRegister_FormID;
        private string _ComplianceRegisterDataFilePath;
        private string _WeldReturnRegisterDataFilePath;
        private string _StringsRegisterDataFilePath;
        private string _ComplianceRegisterDataFileName;
        private string _WeldReturnRegisterDataFileName;
        private string _StringsRegisterDataFileName;

        public FlashButtWeldingWebhookController(IConfiguration configuration, IJotFormService jotFormService)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _jotFormService = jotFormService;
            templateFileName = "Template.xlsx";
            _excelfileendname = "LWRReport.xlsx";
            dailyWeldingReport_jsonMappingFile = "daily_welding_report_fields_mapping.json";
            dailyWeldReturn_jsonMappingFile = "daily_weld_return_fields_mapping.json";
            dailyInspect_jsonMappingFile = "daily_inspect_fields_mapping.json";
            railHeatNumberRecord_jsonMappingFile = "rail_heat_number_record_fields_mapping.json";
            register_jsonMappingFile = "register_path_mapping.json";
            webhookoutputdataFilename = "webhook_data.txt";
            logFileName = "webhook_log.txt";
            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "Flashbutt", logFileName);
            uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "Flashbutt");

            _WeldReturnRegister_XlSheetName = "Weld Return Register";
            _ComplianceRegister_XlSheetName = "Compliance Register";
            _StringsRegister_XlSheetName = "Strings Register";

            _StringsRegisterDataFileName = "Strings_Register_Data.txt";
            _ComplianceRegisterDataFileName = "Compliance_Register_Data.txt";
            _WeldReturnRegisterDataFileName = "Weld_Return_Register_Data.txt";

            _ComplianceRegister_FormID = _configuration["jotformSettings:ComplianceRegisterFormID"];
            _StringsRegister_FormID = _configuration["jotformSettings:StringsRegisterFormID"];
            _WeldReturnRegister_FormID = _configuration["jotformSettings:WeldReturnRegisterFormID"];

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

                logFilePath = Path.Combine(submissionFolder, logFileName);
                string webhookFilePath = Path.Combine(submissionFolder, webhookoutputdataFilename);
                _ComplianceRegisterDataFilePath = Path.Combine(submissionFolder, _ComplianceRegisterDataFileName);
                _WeldReturnRegisterDataFilePath = Path.Combine(submissionFolder, _WeldReturnRegisterDataFileName);
                _StringsRegisterDataFilePath = Path.Combine(submissionFolder, _StringsRegisterDataFileName);
                string excelFilePath = Path.Combine(submissionFolder, $"{DateWiseFolder}_{supervisor}_{_excelfileendname}");

                await GetDataFromJotFormTable(_ComplianceRegister_FormID, _ComplianceRegisterDataFilePath);
                await GetDataFromJotFormTable(_WeldReturnRegister_FormID, _WeldReturnRegisterDataFilePath);
                await GetDataFromJotFormTable(_StringsRegister_FormID, _StringsRegisterDataFilePath);

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

            //// ## This is for Testing 
            //string jsonDataFileWithPath = Path.Combine(submissionFolder, webhookoutputdataFilename);
            //string rawJson = System.IO.File.ReadAllText(jsonDataFileWithPath);
            //var rawArray = rawJson.Split(Environment.NewLine);
            //rawJson = rawArray[rawArray.Length - 1];
            //var webhookData = !string.IsNullOrEmpty(rawJson) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(rawJson) : null;

            //// Compliance Register
            //_ComplianceRegisterDataFilePath = Path.Combine(submissionFolder, _ComplianceRegisterDataFileName);

            //// Weld Return Register
            //_WeldReturnRegisterDataFilePath = Path.Combine(submissionFolder, _WeldReturnRegisterDataFileName);

            //// Strings Register
            //_StringsRegisterDataFilePath = Path.Combine(submissionFolder, _StringsRegisterDataFileName);


            //CopyTemplateToFolder(uploadFolder, excelFilePath);
            //await CreateExcelReportFile(excelFilePath, uploadFolder, webhookData);
            //// ## This is for Testing



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

                    //Rail_Heat_Number_Record_Data
                    worksheet = workbook.Worksheet("Rail_Heat_Number_Record_Data");
                    jsonMappingFileWithPath = Path.Combine(JsonMappingFileFolder, railHeatNumberRecord_jsonMappingFile);
                    Update_Rail_Heat_Number_Record_Data_in_Template(ref worksheet, jsonMappingFileWithPath, webhookData);

                    //Registers
                    worksheet = workbook.Worksheet(_ComplianceRegister_XlSheetName);
                    Write_ComplianceRegisterData_To_Template(ref worksheet, _ComplianceRegisterDataFilePath, _ComplianceRegister_XlSheetName);

                    worksheet = workbook.Worksheet(_WeldReturnRegister_XlSheetName);
                    Write_WeldReturnRegisterData_To_Template(ref worksheet, _WeldReturnRegisterDataFilePath, _WeldReturnRegister_XlSheetName);

                    worksheet = workbook.Worksheet(_StringsRegister_XlSheetName);
                    Write_StringsRegisterData_To_Template(ref worksheet, _StringsRegisterDataFilePath, _StringsRegister_XlSheetName);

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
                            if (item.Value is JObject jObject)
                            {
                                // Loop through JObject properties
                                for (int i = 0; i < data.Count - 2; i++)
                                {
                                    string key = i.ToString();
                                    string value = jObject[key]?.ToString() ?? "";
                                    worksheet.Cell(row, column).Value = value;
                                    current_row = row;
                                    row++;
                                }
                            }
                            else if (item.Value is JArray jArray)
                            {
                                // Loop through JArray elements
                                for (int i = 0; i < jArray.Count; i++)
                                {
                                    string value = jArray[i]?.ToString() ?? "";
                                    worksheet.Cell(row, column).Value = value;
                                    current_row = row;
                                    row++;
                                }
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

                                if (item.Value is JObject jObject)
                                {
                                    // Loop through JObject properties
                                    for (int i = 0; i < data.Count - 2; i++)
                                    {
                                        string key = i.ToString();
                                        string value = jObject[key]?.ToString() ?? "";
                                        worksheet.Cell(row, column).Value = value;
                                        column++;
                                    }
                                }
                                else if (item.Value is JArray jArray)
                                {
                                    // Loop through JArray elements
                                    for (int i = 0; i < jArray.Count; i++)
                                    {
                                        string value = jArray[i]?.ToString() ?? "";
                                        worksheet.Cell(row, column).Value = value;
                                        column++;
                                    }
                                }


                                //var rowValues = (JObject)item.Value;
                                //for (int i = 0; i < rowValues.Count; i++)
                                //{
                                //    string value = rowValues[i.ToString()]?.ToString() ?? ""; // null-safe
                                //    worksheet.Cell(row, column).Value = value;
                                //    column++;
                                //}





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

        private void Write_ComplianceRegisterData_To_Template(ref IXLWorksheet worksheet, string _ComplianceRegisterDataFilePath, string _ComplianceRegister_XlSheetName)
        {
            if (!System.IO.File.Exists(_ComplianceRegisterDataFilePath))
            {
                WriteLog($"Compliance Register data file {_ComplianceRegisterDataFilePath} not found.");
                return;
            }

            WriteJsonRegisterDataToExcel(_ComplianceRegisterDataFilePath, "Compliance_Register_Mapping", ref worksheet, 3, 1);

            //Formatting the Compliance Register data in the worksheet
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null && usedRange.RowCount() > 1)
            {
                // Exclude header (start from second row)
                var dataRange = worksheet.Range(
                    usedRange.FirstCell().Address.RowNumber + 2,  // from row 2
                    usedRange.FirstCell().Address.ColumnNumber,
                    usedRange.LastCell().Address.RowNumber,
                    usedRange.LastCell().Address.ColumnNumber
                );
                var style = dataRange.Cells().Style;
                style.Border.TopBorderColor = XLColor.Black;
                style.Border.SetBottomBorderColor(XLColor.Black);
                style.Border.SetLeftBorderColor(XLColor.Black);
                style.Border.SetRightBorderColor(XLColor.Black);
                style.Border.TopBorder = XLBorderStyleValues.Thin;
                style.Border.BottomBorder = XLBorderStyleValues.Thin;
                style.Border.LeftBorder = XLBorderStyleValues.Thin;
                style.Border.RightBorder = XLBorderStyleValues.Thin;
            }


        }

        private void Write_WeldReturnRegisterData_To_Template(ref IXLWorksheet worksheet, string _WeldReturnRegisterDataFilePath, string _WeldReturnRegister_XlSheetName)
        {
            if (!System.IO.File.Exists(_WeldReturnRegisterDataFilePath))
            {
                WriteLog($"Weld Return Register data file {_WeldReturnRegisterDataFilePath} not found.");
                return;
            }

            WriteJsonRegisterDataToExcel(_WeldReturnRegisterDataFilePath, "Weld_Return_Register_Mapping", ref worksheet, 3, 1);

            //Formatting the Weld Return Register data in the worksheet
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null && usedRange.RowCount() > 1)
            {
                // Exclude header (start from second row)
                var dataRange = worksheet.Range(
                    usedRange.FirstCell().Address.RowNumber + 2,  // from row 2
                    usedRange.FirstCell().Address.ColumnNumber,
                    usedRange.LastCell().Address.RowNumber,
                    usedRange.LastCell().Address.ColumnNumber
                );
                var style = dataRange.Cells().Style;
                style.Border.TopBorderColor = XLColor.Black;
                style.Border.SetBottomBorderColor(XLColor.Black);
                style.Border.SetLeftBorderColor(XLColor.Black);
                style.Border.SetRightBorderColor(XLColor.Black);
                style.Border.TopBorder = XLBorderStyleValues.Thin;
                style.Border.BottomBorder = XLBorderStyleValues.Thin;
                style.Border.LeftBorder = XLBorderStyleValues.Thin;
                style.Border.RightBorder = XLBorderStyleValues.Thin;
            }


        }

        private void Write_StringsRegisterData_To_Template(ref IXLWorksheet worksheet, string _StringsRegisterDataFilePath, string _StringsRegister_XlSheetName)
        {
            if (!System.IO.File.Exists(_StringsRegisterDataFilePath))
            {
                WriteLog($"Strings Register data file {_StringsRegisterDataFilePath} not found.");
                return;
            }

            WriteJsonRegisterDataToExcel(_StringsRegisterDataFilePath, "Strings_Register_Mapping", ref worksheet, 8, 1);

            //Formatting the Strings Register data in the worksheet
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null && usedRange.RowCount() > 1)
            {
                // Exclude header (start from second row)
                var dataRange = worksheet.Range(
                    usedRange.FirstCell().Address.RowNumber + 7,  // from row 8
                    usedRange.FirstCell().Address.ColumnNumber,
                    usedRange.LastCell().Address.RowNumber,
                    usedRange.LastCell().Address.ColumnNumber
                );
                var style = dataRange.Cells().Style;
                style.Border.TopBorderColor = XLColor.Black;
                style.Border.SetBottomBorderColor(XLColor.Black);
                style.Border.SetLeftBorderColor(XLColor.Black);
                style.Border.SetRightBorderColor(XLColor.Black);
                style.Border.TopBorder = XLBorderStyleValues.Thin;
                style.Border.BottomBorder = XLBorderStyleValues.Thin;
                style.Border.LeftBorder = XLBorderStyleValues.Thin;
                style.Border.RightBorder = XLBorderStyleValues.Thin;
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


        private async Task GetDataFromJotFormTable(string _Register_FormID, string _OutPutJsonFile)
        {
            var formFields = await _jotFormService.GetFormFieldsAsync(_Register_FormID);
            var formTableData = await _jotFormService.GetSubmissionsAsync(_Register_FormID);

            // Filter valid fields (replace with your actual validation logic)
            var keys = new List<string>();
            List<JotFormField> formFieldsHeaderList = new List<JotFormField>();
            foreach (var kvp in formFields)
            {
                string kvpValueJsonString = JsonConvert.SerializeObject(kvp.Value);
                if (IsValidField(kvpValueJsonString))
                {
                    keys.Add(kvp.Key);
                    JotFormField formField = JsonConvert.DeserializeObject<JotFormField>(kvpValueJsonString);
                    formFieldsHeaderList.Add(formField);
                }
            }

            // Build JSON-friendly structure
            JotFormOutputExcelData outputData = new JotFormOutputExcelData
            {
                Headers = keys.ConvertAll(k => formFields[k].ToString()),
                HeaderList = formFieldsHeaderList,
                Rows = new List<Dictionary<string, object>>()
            };

            foreach (var sub in formTableData)
            {
                var row = new Dictionary<string, object>
                    {
                        { "Id", sub.id }
                    };

                foreach (var key in keys)
                {
                    JotFormField formField = new JotFormField()
                    {
                        text = formFields[key]?.ToString() ?? "",
                        type = formFields[key]?.GetType().Name.ToLowerInvariant() ?? "unknown"
                    };
                    
                    var field = formField;
                    object value = sub.answers.ContainsKey(key) ? sub.answers[key] : "";

                    //if (field.type == "control_datetime" && value is JObject dateObj &&
                    //        dateObj["day"] != null && dateObj["month"] != null && dateObj["year"] != null &&
                    //        int.TryParse(dateObj["day"]?.ToString(), out int day) &&
                    //        int.TryParse(dateObj["month"]?.ToString(), out int month) &&
                    //        int.TryParse(dateObj["year"]?.ToString(), out int year))
                    //{
                    //    // Format date to dd/MM/yyyy or as DateTime
                    //    DateTime date = new DateTime(year, month, day);
                    //    value = date.ToString("yyyy-MM-dd");
                    //}
                    //else if (value is DateTime dt2)
                    //{
                    //    value = dt2.ToString("yyyy-MM-dd");
                    //}
                    
                    row[key] = value;
                }

                outputData.Rows.Add(row);
            }

            // Serialize to JSON    
            var json = JsonConvert.SerializeObject(outputData, Formatting.Indented);

            // Write to file
            System.IO.File.WriteAllText(_OutPutJsonFile, json);

            return;
        }


        static bool IsValidField(string field)
        {
            var validTypes = new HashSet<string>
                {
                    "control_textbox", "control_textarea", "control_number","control_time",
                    "control_datetime", "control_radio", "control_checkbox", "control_dropdown"
                };

            // Now deserialize into your class
            JotFormField jffield = JsonConvert.DeserializeObject<JotFormField>(field);
            return validTypes.Contains(jffield.type);

        }
                
        public void WriteJsonRegisterDataToExcel(string jsonFilePath, string xlJsonMappingSheet, ref IXLWorksheet excelWorksheet, int _dataStartRow, int _dataStartCol)
        {
            // Read JSON file
            var jsonContent = System.IO.File.ReadAllText(jsonFilePath);
            var selectedHeaders = new Dictionary<int, string>();

            var mappingworksheet = excelWorksheet.Workbook.Worksheet(xlJsonMappingSheet);
            int lastRow = mappingworksheet.LastRowUsed().RowNumber();
            int j = 0;
            for (int row = 2; row <= lastRow; row++) // Start from row 2 (skip header)
            {
                string fieldName = mappingworksheet.Cell(row, 2).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    selectedHeaders.Add(j++, fieldName);
                }
            }

            // Deserialize into object
            var databeforesort = JsonConvert.DeserializeObject<JotFormOutputExcelData>(jsonContent);
            List<JotFormField> data = new List<JotFormField>();

            if (selectedHeaders.Count > 0)
            {
                foreach (var header in selectedHeaders)
                {
                    var res = databeforesort.HeaderList.Where(x => x.name == header.Value).ToList();
                    if (res.Any())
                    {
                        data.AddRange(res);
                    }
                }

                if (data.Any() && data.Count > 0)
                {

                    for (int r = 0; r < databeforesort.Rows.Count; r++)
                    {
                        var rowDict = databeforesort.Rows[r];
                        int col = 0;
                        foreach (var header in data)
                        {
                            string colheader = header.qid; 
                            rowDict.TryGetValue(colheader, out var rawValue);
                            object cellValue = null;
                                                        
                            if (rawValue is JObject jsonObj)
                            {
                                if(jsonObj.ContainsKey("answer") && jsonObj["answer"] is JObject dateObj)
                                {
                                    if (dateObj["day"] != null && dateObj["month"] != null && dateObj["year"] != null &&
                                            int.TryParse(dateObj["day"]?.ToString(), out int day) &&
                                            int.TryParse(dateObj["month"]?.ToString(), out int month) &&
                                            int.TryParse(dateObj["year"]?.ToString(), out int year))
                                    {
                                        // Format date to dd/MM/yyyy or as DateTime
                                        DateTime date = new DateTime(year, month, day);
                                        cellValue = date.ToString("dd-MM-yyyy");
                                    }
                                    else if (dateObj["hourSelect"] != null && dateObj["minuteSelect"] != null &&
                                            int.TryParse(dateObj["hourSelect"]?.ToString(), out int hour) &&
                                            int.TryParse(dateObj["minuteSelect"]?.ToString(), out int min))
                                    {
                                        cellValue = hour.ToString("D2") + ":" + min.ToString("D2");
                                    }
                                }
                                else if (jsonObj.ContainsKey("answer") && jsonObj["answer"]?.Type == JTokenType.String)
                                {
                                    cellValue = (string)jsonObj["answer"];
                                }
                            }
                            
                            excelWorksheet.Cell(_dataStartRow + r, _dataStartCol + col++).Value = cellValue?.ToString() ?? "";

                        }
                    }
                }
            }

        }

    }

}
