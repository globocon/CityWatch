using CityWatch.Data.Providers;
using CityWatch.Data;

namespace CityWatch.RadioCheck.Services
{
    public interface IViewDataService
    {
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

    }
}
