
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using CityWatch.RadioCheck.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using Microsoft.Extensions.Logging;
using System;
using CityWatch.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using CityWatch.RadioCheck.Helpers;
using CityWatch.RadioCheck.Models;
using System.Linq;
using iText.Commons.Actions.Contexts;
using static Dropbox.Api.Files.SearchMatchType;
using Dropbox.Api.Team;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;

namespace CityWatch.RadioCheck.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RadioChecksSWReaderController : ControllerBase
    {
        private readonly IRadioChecksActivityStatusService _radioChecksActivityStatusService;
        private readonly CityWatchDbContext _context;
        private readonly Settings _settings;
        public RadioChecksSWReaderController(IRadioChecksActivityStatusService radioChecksActivityStatusService, CityWatchDbContext context)
        {
            _radioChecksActivityStatusService = radioChecksActivityStatusService;
            _context = context;
        }

        [Route("[action]", Name = "RadioChecksSWReader")]
        [HttpGet]
        public async Task<string> RadioChecksSWReader()
        {

            try
            {
                var actionResult = await GetDataAndSave();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Error";
            }


        }

        public async Task<IActionResult> GetDataAndSave()
        {
            try
            {
                RemoveUnusedScanDetails();
                    /* Remove the SW read more than 2 hours Start */
                    var checkIfGreaterThan2hours = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ActivityType == "SW").ToList();
                if (checkIfGreaterThan2hours != null)
                {
                    if (checkIfGreaterThan2hours.Count != 0)
                    {
                        foreach (var ClientSiteRadioChecksActivity in checkIfGreaterThan2hours)
                        {
                            if (ClientSiteRadioChecksActivity.LastSWCreatedTime != null)
                            {
                                var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastSWCreatedTime).Value.TotalHours < 2;
                                if (!isActive)
                                {
                                    /* Remove from ClientSiteRadioChecksActivityStatus*/
                                    _context.ClientSiteRadioChecksActivityStatus.Remove(ClientSiteRadioChecksActivity);
                                    _context.SaveChanges();

                                    var removefromSmartWandScanResults = _context.RadioChecksSmartWandScanResults.Where(x => x.Id == ClientSiteRadioChecksActivity.SWId).ToList();

                                    if (removefromSmartWandScanResults != null)
                                    {
                                        if (removefromSmartWandScanResults.Count != 0)
                                        { //remove from RadioChecksSmartWandScanResults
                                            _context.RadioChecksSmartWandScanResults.RemoveRange(removefromSmartWandScanResults);
                                            _context.SaveChanges();
                                        }
                                    }


                                }
                            }
                        }
                    }

                }
                /*remove the old datas from SmartWandScanResults >2*/
                //var allList = _context.RadioChecksSmartWandScanResults.ToList();
                //if (allList != null)
                //{
                //    if (allList.Count != 0)
                //    {
                //        foreach (var itemsList in allList)
                //        {
                //            if (itemsList.InspectionStartDatetimeLocal != null)
                //            {
                //                var isActive = (DateTime.Now - itemsList.InspectionStartDatetimeLocal).TotalHours < 2;
                //                if (!isActive)
                //                {
                //                    _context.RadioChecksSmartWandScanResults.Remove(itemsList);
                //                    _context.SaveChanges();
                //                }
                //            }
                //        }
                //    }

                //}
                /* Remove the SW read more than 2 hours end */


                var results = new RootObject();
                using (var client = new HttpClient())
                {
                    var newstr = _context.SWChannel.SingleOrDefault().SWChannel;

                    var url = newstr + $"/?start_datetime={DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss")}&include_templates=11";

                    // var url = $"https://api.koios.pl/kms-api/v2/inspections/scan/?start_datetime={DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss")}&include_templates=11";
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Authorization", "814efaaa61e5a5fdddcf8e5c7bee32df4c7bc8657fadce203330081f9d262e1f");
                    request.Headers.Add("Agency", "citywatch");
                    request.Headers.Add("MediaTypeWithQuality", "application/json");
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {

                        var resultString = await response.Content.ReadAsStringAsync();
                        results = JsonSerializer.Deserialize<RootObject>(resultString);


                    }



                }


                if (results != null)
                {
                    if (results.results != null)
                    {
                        if (results.results.Length > 0)
                        {
                            foreach (var swScanItem in results.results)
                            {
                                /* Check if the template name is '02  ******  ON PATROL  ******'*/
                                //if (swScanItem.TemplateIdentificationNumber == "11")
                                //{
                                if (swScanItem.EmployeePhone != null)
                                {
                                    /* Compare the phone number from swScanItem with ClientSiteSmartWands phone number remove all space and take right 9 char */
                                    var comparisonString = TakeRightCharacters(RemoveWhitespace(swScanItem.EmployeePhone), 9);
                                    var smartWandDetails = _context.ClientSiteSmartWands
                                   .AsEnumerable()
                                   .Where(x => TakeRightCharacters(RemoveWhitespace(x.PhoneNumber), 9) == comparisonString)
                                   .FirstOrDefault();

                                    if (smartWandDetails != null)
                                    {
                                        /* find the siteId and Smartwand Id using Phone number */
                                        if (smartWandDetails.ClientSiteId != null && smartWandDetails.SmartWandId != null)
                                        {
                                            /* find the GuardId that curresponding to the siteId and SmartwandId*/
                                            var latestRecord = _context.GuardLogins.Where(x => x.ClientSiteId == smartWandDetails.ClientSiteId && x.SmartWandId == smartWandDetails.Id).OrderByDescending(e => e.LoginDate).FirstOrDefault();

                                            if (latestRecord != null)
                                            {
                                                /* remove the old data check if the same ClientSiteId , GuardId and SmartWandId 
                                                 then the table contain latest scan details for a site
                                                 */
                                                //var oldSameData = _context.RadioChecksSmartWandScanResults.Where(x => x.ClientSiteId == smartWandDetails.ClientSiteId && x.GuardId == latestRecord.GuardId && x.SmartWandId == smartWandDetails.SmartWandId).ToList();
                                                //if (oldSameData != null)
                                                //{
                                                //    if (oldSameData.Count != 0)
                                                //    {
                                                //        _context.RadioChecksSmartWandScanResults.RemoveRange(oldSameData);
                                                //        _context.SaveChanges();
                                                //    }

                                                //}


                                                /* Save the details to RadioChecksSmartWandScanResults*/
                                                var RadioChecksSmartWandScanResults = new RadioChecksSmartWandScanResults()
                                                {
                                                    Id = swScanItem.Id,
                                                    EmployeeId = swScanItem.EmployeeId,
                                                    EmployeeName = swScanItem.EmployeeName,
                                                    EmployeePhone = swScanItem.EmployeePhone,
                                                    TemplateId = swScanItem.TemplateId,
                                                    TemplateIdentificationNumber = swScanItem.TemplateIdentificationNumber,
                                                    TemplateName = swScanItem.TemplateName,
                                                    ClientId = swScanItem.ClientId,
                                                    SiteId = swScanItem.SiteId,
                                                    SiteName = swScanItem.SiteName,
                                                    LocationId = swScanItem.LocationId,
                                                    LocationName = swScanItem.LocationName,
                                                    LocationScan = swScanItem.LocationScan,
                                                    InspectionStartDatetimeLocal = Convert.ToDateTime(swScanItem.InspectionStartDatetimeLocal),
                                                    InspectionEndDatetimeLocal = Convert.ToDateTime(swScanItem.InspectionEndDatetimeLocal),
                                                    ClientSiteId = smartWandDetails.ClientSiteId,
                                                    GuardId = latestRecord.GuardId,
                                                    SmartWandId = smartWandDetails.SmartWandId
                                                };
                                                _context.RadioChecksSmartWandScanResults.Add(RadioChecksSmartWandScanResults);
                                                _context.SaveChanges();



                                                /* Remove Old data >2*/
                                                //var oldSameDataInRc = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == smartWandDetails.ClientSiteId && x.GuardId == latestRecord.GuardId && x.ActivityType == "SW").ToList();
                                                //if (oldSameDataInRc != null)
                                                //{
                                                //    if (oldSameDataInRc.Count != 0)
                                                //    {
                                                //        _context.ClientSiteRadioChecksActivityStatus.RemoveRange(oldSameDataInRc);
                                                //        _context.SaveChanges();
                                                //    }

                                                //}
                                                /* Save the details to ClientSiteRadioChecksActivityStatus*/
                                                var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                                                {
                                                    ClientSiteId = smartWandDetails.ClientSiteId,
                                                    GuardId = latestRecord.GuardId,
                                                    LastSWCreatedTime = Convert.ToDateTime(swScanItem.InspectionStartDatetimeLocal),
                                                    SWId = swScanItem.Id,
                                                    ActivityType = "SW",
                                                    ActivityDescription = swScanItem.TemplateName
                                                };

                                                _context.ClientSiteRadioChecksActivityStatus.Add(clientsiteRadioCheck);
                                                _context.SaveChanges();



                                            }

                                        }
                                    }

                                }

                                //}


                            }



                            // Code to keep single history of last SW activity of Guard - Binoy 04-04-2024 - Start

                            var newdata = _context.RadioChecksSmartWandScanResults
                                .OrderBy (x => x.InspectionStartDatetimeLocal)
                                .ToList();
                            foreach(var newrecord in newdata)
                            {
                                // check if same record exists if not insert else update
                                var existingrecord = _context.SmartWandScanGuardHistory.Where(x=> x.GuardId == newrecord.GuardId && x.ClientSiteId == newrecord.ClientSiteId).FirstOrDefault();
                                if(existingrecord != null)
                                {
                                    if(newrecord.InspectionStartDatetimeLocal > existingrecord.InspectionStartDatetimeLocal)
                                    {
                                        // update existing record
                                        // existingrecord.Id = newrecord.Id;
                                        existingrecord.EmployeeId = newrecord.EmployeeId;
                                        existingrecord.EmployeeName = newrecord.EmployeeName;
                                        existingrecord.EmployeePhone = newrecord.EmployeePhone;
                                        existingrecord.TemplateId = newrecord.TemplateId;
                                        existingrecord.TemplateIdentificationNumber = newrecord.TemplateIdentificationNumber;
                                        existingrecord.TemplateName = newrecord.TemplateName;
                                        existingrecord.ClientId = newrecord.ClientId;
                                        existingrecord.SiteId = newrecord.SiteId;
                                        existingrecord.SiteName = newrecord.SiteName;
                                        existingrecord.LocationId = newrecord.LocationId;
                                        existingrecord.LocationName = newrecord.LocationName;
                                        existingrecord.InspectionStartDatetimeLocal = newrecord.InspectionStartDatetimeLocal;
                                        existingrecord.InspectionEndDatetimeLocal = newrecord.InspectionEndDatetimeLocal;
                                        existingrecord.ClientSiteId = newrecord.ClientSiteId;
                                        existingrecord.GuardId = newrecord.GuardId;
                                        existingrecord.SmartWandId = newrecord.SmartWandId;
                                        existingrecord.LocationScan = newrecord.LocationScan;
                                        existingrecord.RecordLastUpdateTime = DateTime.Now;
                                        _context.SmartWandScanGuardHistory.Update(existingrecord);
                                        _context.SaveChanges();
                                    }
                                }
                                else
                                {
                                    // Insert new record
                                    SmartWandScanGuardHistory nwrecord = new SmartWandScanGuardHistory()
                                    {
                                        //Id = newrecord.Id,
                                        EmployeeId = newrecord.EmployeeId,
                                        EmployeeName = newrecord.EmployeeName,
                                        EmployeePhone = newrecord.EmployeePhone,
                                        TemplateId = newrecord.TemplateId,
                                        TemplateIdentificationNumber = newrecord.TemplateIdentificationNumber,
                                        TemplateName = newrecord.TemplateName,
                                        ClientId = newrecord.ClientId,
                                        SiteId = newrecord.SiteId,
                                        SiteName = newrecord.SiteName,
                                        LocationId = newrecord.LocationId,
                                        LocationName = newrecord.LocationName,
                                        InspectionStartDatetimeLocal = newrecord.InspectionStartDatetimeLocal,
                                        InspectionEndDatetimeLocal = newrecord.InspectionEndDatetimeLocal,
                                        ClientSiteId = newrecord.ClientSiteId,
                                        GuardId = newrecord.GuardId,
                                        SmartWandId = newrecord.SmartWandId,
                                        LocationScan = newrecord.LocationScan,
                                        RecordCreateTime = DateTime.Now
                                    };

                                    _context.SmartWandScanGuardHistory.Add(nwrecord);
                                    _context.SaveChanges();
                                }
                            }

                            // Code to keep single history of last SW activity of Guard - Binoy 04-04-2024 - End
                        }

                    }
                }

                return Ok("Data saved successfully");
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }




        public void  RemoveUnusedScanDetails()
        {
            /* This sp removes unwanted smart want result from the SmartWandScanResults 10062024*/
          
            _context.Database.ExecuteSql($"EXEC Sp_deleteUnWantedrecordsFromSWResults");
   

        }
        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public string TakeRightCharacters(string input, int count)
        {
            // Ensure count is not greater than the length of the input
            count = Math.Min(count, input.Length);
            // Get the rightmost characters
            string rightCharacters = input.Substring(input.Length - count);
            return rightCharacters;
        }
    }
}
