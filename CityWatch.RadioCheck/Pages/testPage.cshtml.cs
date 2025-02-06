using CityWatch.Data;
using CityWatch.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CityWatch.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.Pages
{
    public class testPageModel : PageModel
    {

        private readonly CityWatchDbContext _context;
        public List<UserInput> UserInputs { get; set; } = new List<UserInput>();
        public testPageModel(CityWatchDbContext context)
        {
            _context = context;
        }
        public async Task OnGetAsync()
        {
            UserInputs = await _context.UserInput
                                   .OrderByDescending(u => u.UpdatedDate)
                                   .ToListAsync();

        }
    }
}
