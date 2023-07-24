using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CityWatch.Kpi.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportDataController : ControllerBase
    {
        private readonly IImportDataService _importDataService;

        public ImportDataController(IImportDataService importDataService)
        {
            _importDataService = importDataService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            return DateTime.Now.ToString();
        }
    }
}
