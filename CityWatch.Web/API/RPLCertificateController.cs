using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RPLCertificateController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IRPLCertificateGeneratorService _rplCertificateGeneratorService;
        private readonly ILogger<RPLCertificateController> _logger;
        public RPLCertificateController(IRPLCertificateGeneratorService rplCertificateGeneratorService,
          IWebHostEnvironment webHostEnvironment,
            ILogger<RPLCertificateController> logger)
        {
            _rplCertificateGeneratorService = rplCertificateGeneratorService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [Route("[action]", Name = "RPLCertificate")]
        [HttpGet]
        public bool RPLCertificate()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Not supported in development environment");
            try
            {
                _rplCertificateGeneratorService.GenerateRPLCertificate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return true;
        }
    }
}
