using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityWatch.Kpi.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class CleanupController : ControllerBase
    {
        private readonly ICleanupService _cleanupService;

        public CleanupController(ICleanupService cleanupService)
        {
            _cleanupService = cleanupService;
        }

        [HttpGet]
        public JsonResult Get()
        {
            try
            {
                _cleanupService.DeleteKpiReports();
                _cleanupService.DeleteLogs();
            }
            catch
            {
            }
            return new JsonResult(true);
        }
    }
}
