using CityWatch.Data.Providers;
using CityWatch.Data;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System.Collections.Generic;
using System.Linq;
using CityWatch.Web.Models;

namespace CityWatch.RadioCheck.Services
{
    public interface IViewDataService
    {
        List<GuardViewModel> GetGuards();
        List<GuardViewModel> GetActiveGuards();
    }

    public class ViewDataService : IViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IKpiDataProvider _kpiDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly CityWatchDbContext _context;

        public ViewDataService(IClientDataProvider clientDataProvider,
            IKpiDataProvider kpiDataProvider,
            IConfigDataProvider configDataProvider,
            IGuardDataProvider guardDataProvider, CityWatchDbContext context)
        {
            _clientDataProvider = clientDataProvider;
            _kpiDataProvider = kpiDataProvider;
            _configDataProvider = configDataProvider;
            _guardDataProvider = guardDataProvider;
            _context = context;
        }
        public List<GuardViewModel> GetGuards()
        {
            var guards = _guardDataProvider.GetGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        }
        public List<GuardViewModel> GetActiveGuards()
        {
            var guards = _guardDataProvider.GetActiveGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        }
        //public List<KeyVehicleLog> GetCompanyDetails()
        //{
        //    //var guards = _guardDataProvider.GetEmailPOCVehiclelog( id);
        //    //var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
        //    //return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        //}
    }
}
