using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteRadioStatusDataProvider
    {
        List<ClientSiteRadioStatus> GetClientSiteRadioStatus(int[] clientSiteIds, DateTime fromDate);
        void SaveClientSiteRadioStatus(IEnumerable<ClientSiteRadioStatus> clientSiteRadioStatus);
        void SaveClientSiteRadioCheck(int clientSiteId, DateTime checkDate, int checkNumber, string newValue);
    }

    public class ClientSiteRadioStatusDataProvider : IClientSiteRadioStatusDataProvider
    {
        private readonly CityWatchDbContext _context;

        public ClientSiteRadioStatusDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<ClientSiteRadioStatus> GetClientSiteRadioStatus(int[] clientSiteIds, DateTime fromDate)
        {
            return _context.ClientSiteRadioStatus
                .Where(z => clientSiteIds.Contains(z.ClientSiteId) && z.CheckDate >= fromDate && z.CheckDate < fromDate.AddDays(7))
                .Include(z => z.ClientSite)
                .ToList();
        }

        public void SaveClientSiteRadioStatus(IEnumerable<ClientSiteRadioStatus> clientSiteRadioStatus)
        {
            if (!clientSiteRadioStatus.Any())
                return;

            if (clientSiteRadioStatus.Any(z => z.Id != 0))
                throw new NotImplementedException("Update not supported");

            _context.ClientSiteRadioStatus.AddRange(clientSiteRadioStatus);
            _context.SaveChanges();
        }

        public void SaveClientSiteRadioCheck(int clientSiteId, DateTime checkDate, int checkNumber, string newValue)
        {
            var recordToUpdate = _context.ClientSiteRadioStatus.Where(z => z.ClientSiteId == clientSiteId && z.CheckDate == checkDate).SingleOrDefault();
            if (recordToUpdate != null)
            {
                if (checkNumber == 1) recordToUpdate.Check1 = newValue;
                else if (checkNumber == 2) recordToUpdate.Check2 = newValue;
                else if (checkNumber == 3) recordToUpdate.Check3 = newValue;

                _context.SaveChanges();
            }
        }
    }
}
