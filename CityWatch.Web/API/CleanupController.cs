using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityWatch.Web.API
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
                _cleanupService.DeleteArchivedPdfs();
                _cleanupService.DeleteLogs();
                _cleanupService.DeletePatrolDataExcels();
            }
            catch
            { }
            return new JsonResult(true);
        }

        [Route("[action]", Name = "GpsImage")]
        [HttpPost]
        public JsonResult GpsImage()
        {
            try
            {
                _cleanupService.RecreateGpsImage();
            }
            catch
            { }
            return new JsonResult(true);
        }
    }
}
