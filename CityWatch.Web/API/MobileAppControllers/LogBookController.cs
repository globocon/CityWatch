using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogBookController : ControllerBase
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IClientDataProvider _clientSitesDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly ILogbookDataService _logbookDataService;
        public LogBookController(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider,
            IClientDataProvider clientSitesDataProvider, IGuardDataProvider guardDataProvider, ILogbookDataService logbookDataService)
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientSitesDataProvider = clientSitesDataProvider;
            _guardDataProvider = guardDataProvider;
            _logbookDataService = logbookDataService;
        }

        [HttpGet("GetCustomFieldConfig")]
        public IActionResult GetCustomFieldConfig(int siteId)
        {
            try
            {
                var _ClientSiteCustomFieldConfig = _viewDataService.GetCustomFieldConfig(siteId);
                //return new JsonResult(columns.ToArray());
                return Ok(_ClientSiteCustomFieldConfig.ToArray());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("GetCustomFieldLogs")]
        public IActionResult GetCustomFieldLogs(int siteId)
        {
            try
            {
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(siteId, logBookType);
                var rows = _viewDataService.GetCustomFieldLogs(logBookId, siteId);
                //return new JsonResult(rows.ToArray());
                return Ok(rows.ToArray());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


        [HttpPost("SaveCustomFieldLog")]
        public IActionResult SaveCustomFieldLog([FromBody] CustomFieldLogRequest request)
        {
            bool isSuccess = false;
            string Message = string.Empty;

            try
            {
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.SiteId, logBookType);

                isSuccess = _viewDataService.SaveCustomFieldLog(logBookId, request.Records);
                Message = "Saved successfully.";
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return Ok(new { IsSuccess = isSuccess, message = Message });
        }


        [HttpGet("GetPatrolCarLogs")]
        public IActionResult GetPatrolCarLogs(int siteId)
        {
            try
            {
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(siteId, logBookType);
                var patrolCarLogs = _viewDataService.GetPatrolCarLogs(logBookId, siteId);
                //return new JsonResult(patrolCarLogs);
                return Ok(patrolCarLogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


        [HttpPost("SavePatrolCarLog")]
        public IActionResult SavePatrolCarLog([FromBody] PatrolCarLogRequest request)
        {
            var isSuccess = false;
            var Message = string.Empty;
            try
            {
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.SiteId, logBookType);
                isSuccess = _viewDataService.SavePatrolCarLog(request.Records);
                Message = "Saved successfully.";
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return Ok(new { IsSuccess = isSuccess, message = Message });
        }


    }

    public class CustomFieldLogRequest
    {
        public int SiteId { get; set; }
        public Dictionary<string, string> Records { get; set; }
    }

    public class PatrolCarLogRequest
    {
        public int SiteId { get; set; }
        public PatrolCarLog Records { get; set; }
    }
}
