using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CityWatch.Web.Pages.Radio
{
    public class NonActiveGuardsModel : PageModel
    {

        
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public NonActiveGuardsModel( IGuardLogDataProvider guardLogDataProvider)
        {
          
            _guardLogDataProvider= guardLogDataProvider;
        }
        public void OnGet()
        {
        }

        public IActionResult OnGetClientSiteActivityStatus(string clientSiteIds)
        {
           
            return new JsonResult(_guardLogDataProvider.GetActiveGuardDetails());
        }

        public IActionResult OnGetClientSiteInActivityStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetInActiveGuardDetails());
        }
         //for getting logBookDetails of Guards-start
        public IActionResult OnGetClientSitelogBookActivityStatus(int clientSiteId,int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardlogBookDetails(clientSiteId,guardId));
        }

        //for getting logBookDetails of Guards-end
    }
}
