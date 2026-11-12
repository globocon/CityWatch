using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteActivityStatusDataProvider
    {
        List<ClientSiteActivityStatus> GetClientSiteActivityStatus(int[] clientSiteIds);
        ClientSiteRadioCheck GetLatestClientSiteRadioCheck(int clientSiteId, int guardId);
        void SaveClientSiteActivityStatus(IEnumerable<ClientSiteActivityStatus> clientSiteActivityStatus);
        void SaveClientSiteActivityStatus(ClientSiteActivityStatus clientSiteActivityStatus);
        void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck);
        void DeleteAllClientSiteActivityStatus();
    }

    public class ClientSiteActivityStatusDataProvider : IClientSiteActivityStatusDataProvider
    {
        private readonly CityWatchDbContext _context;

        public ClientSiteActivityStatusDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<ClientSiteActivityStatus> GetClientSiteActivityStatus(int[] clientSiteIds)
        {
            return _context.ClientSiteActivityStatus
                .Where(z => !clientSiteIds.Any() || clientSiteIds.Contains(z.ClientSiteId))
                .Include(z => z.ClientSite)
                .Include(z => z.Guard)
                .ToList();
        }

        public void SaveClientSiteActivityStatus(IEnumerable<ClientSiteActivityStatus> clientSiteActivityStatus)
        {
            _context.ClientSiteActivityStatus.AddRange(clientSiteActivityStatus);
            _context.SaveChanges();
        }

        public void SaveClientSiteActivityStatus(ClientSiteActivityStatus clientSiteActivityStatus)
        {
            if (clientSiteActivityStatus.Id == 0)
            {
                _context.ClientSiteActivityStatus.Add(clientSiteActivityStatus);
            }
            else
            {
                var statusToUpdate = _context.ClientSiteActivityStatus.SingleOrDefault(z => z.Id == clientSiteActivityStatus.Id);
                if (statusToUpdate != null)
                {
                    statusToUpdate.LastActiveSrcId = clientSiteActivityStatus.LastActiveSrcId;
                    statusToUpdate.LastActiveAt = clientSiteActivityStatus.LastActiveAt;
                    statusToUpdate.LastActiveDescription = clientSiteActivityStatus.LastActiveDescription;
                    statusToUpdate.Status = clientSiteActivityStatus.Status;
                }
            }
            _context.SaveChanges();
        }

        public void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck)
        {
            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
            _context.SaveChanges();
        }

        public void DeleteAllClientSiteActivityStatus()
        {
            var clientSiteActivityStatus = _context.ClientSiteActivityStatus.ToList();
            if (clientSiteActivityStatus.Any())
            {
                _context.ClientSiteActivityStatus.RemoveRange(clientSiteActivityStatus);
                _context.SaveChanges();
            }
        }

        public ClientSiteRadioCheck GetLatestClientSiteRadioCheck(int clientSiteId, int guardId)
        {
            return _context.ClientSiteRadioChecks
                            .Where(z => z.ClientSiteId == clientSiteId && z.GuardId == guardId)
                            .OrderBy(z => z.CheckedAt)
                            .LastOrDefault();
        }

       
    }
}
