using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface IAppConfigurationProvider
    {
        void SaveConfiguration(AppConfiguration appConfiguration);
        AppConfiguration GetConfigurationByName(string name);
        List<AppConfiguration> GetConfigurations();
    }

    public class AppConfigurationProvider : IAppConfigurationProvider
    {
        private readonly CityWatchDbContext _context;

        public AppConfigurationProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public AppConfiguration GetConfigurationByName(string name)
        {
           return _context.Appconfigurations.SingleOrDefault(x => x.Name == name);
        }

        public List<AppConfiguration> GetConfigurations()
        {
            return _context.Appconfigurations.ToList(); 
        }

        public void SaveConfiguration(AppConfiguration appConfiguration)
        {
            if(appConfiguration == null)
                throw new ArgumentNullException();

            var appConfigurationToUpdate = _context.Appconfigurations.SingleOrDefault(x => x.Id == appConfiguration.Id);
            if(appConfigurationToUpdate != null)
            {
                appConfigurationToUpdate.Value = appConfiguration.Value;
                _context.SaveChanges();
            }
        }
    }
}
