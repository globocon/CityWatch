using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;


namespace CityWatch.Web.API
{


    [Route("api/[controller]")]
    [ApiController]
    public class GuardSecurityNumberController : ControllerBase
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;

        public GuardSecurityNumberController(IGuardDataProvider guardDataProvider, IViewDataService viewDataService)
        {
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
        }

        [HttpGet("GetGuardDetails/{securityNumber}")]
        public IActionResult GetGuardDetails(string securityNumber)
        {
            if (string.IsNullOrWhiteSpace(securityNumber))
                return BadRequest(new { message = "Security number is required." });

            var guard = _guardDataProvider.GetGuards()
                .SingleOrDefault(z => string.Compare(z.SecurityNo, securityNumber, StringComparison.OrdinalIgnoreCase) == 0);

            if (guard == null)
            {
                return NotFound(new
                {
                    message = "A guard with given security license number not found. If you are a new guard, tick 'New Guard?' to register and login.",
                    isActive = false
                });
            }

            if (!guard.IsActive)
            {
                return Unauthorized(new
                {
                    message = "A guard with given security license number is disabled. Please contact admin to activate.",
                    isActive = false
                });
            }

            return Ok(new
            {
                GuardId = guard.Id,
                Name = guard.Name,
                SecurityNo = guard.SecurityNo,
                isActive = true
            });
        }


        [HttpGet("GetUserClientTypes")]
        public IActionResult GetUserClientTypes(int userId, int? clientTypeId = null)
        {
            try
            {
                var clientTypes = _viewDataService.GetUserClientTypesCountWithTypeId(userId, clientTypeId);

                if (clientTypes == null || !clientTypes.Any())
                    return NotFound(new { message = "No client types found." });

                return Ok(clientTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

    }
}
