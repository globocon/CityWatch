using CityWatch.Data;
using CityWatch.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {

        private readonly CityWatchDbContext _context;
        public SyncController(CityWatchDbContext context)
        {
            _context = context;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncData([FromBody] SyncData syncData)
        {
            if (syncData?.Data == null || !syncData.Data.Any())
            {
                return BadRequest("Invalid data.");
            }

            foreach (var userInput in syncData.Data)
            {
                // Remove Id for new inserts
                var newUserInput = new UserInput
                {
                    Text = userInput.Text,
                    UpdatedDate = DateTime.Now
                };

                _context.UserInput.Add(newUserInput);
            }

            await _context.SaveChangesAsync();
            return Ok("Data synced successfully.");
        }
    }

    public class SyncData
    {
        public List<UserInput> Data { get; set; }
    }
}
