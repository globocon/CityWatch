using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IGuardSettingsDataProvider
    {
        // TODO: other guard settings related functions - move here

        // Site PoCs and Locations

        List<ClientSitePoc> GetClientSitePocs(int clientSiteId);
        List<ClientSitePoc> GetClientSitePocs(int[] clientSiteIds);
        void SaveClientSitePoc(ClientSitePoc clientSitePoc);
        void DeleteClientSitePoc(int id);
        List<ClientSiteLocation> GetClientSiteLocations(int clientSiteId);
        List<ClientSiteLocation> GetClientSiteLocations(int[] clientSiteIds);
        void SaveClientSiteLocation(ClientSiteLocation clientSiteLocation);
        void DeleteClientSiteLocation(int id);

        // Keys

        public List<ClientSiteKey> GetClientSiteKeys(int clientSiteId);
        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds);
        public void SaveClientSiteKey(ClientSiteKey clientSiteKey);
        void DeleteClientSiteKey(int id);
        //p2-140 key photos  -start
        void DeleteClientSiteKeyImage(int id);
        //p2-140 key photos  -end
        public void SaveANPR(ANPR anpr);
        public List<ANPR> GetANPR(int clientSiteId);
        public void DeleteANPR(int id);
        public ANPR GetANPRCheckbox(int clientSiteId);
    }

    public class GuardSettingsDataProvider : IGuardSettingsDataProvider
    {
        private readonly CityWatchDbContext _context;

        public GuardSettingsDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<ClientSitePoc> GetClientSitePocs(int clientSiteId)
        {
            return _context.ClientSitePocs
                .Where(z => z.ClientSiteId == clientSiteId && !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }

        public List<ClientSitePoc> GetClientSitePocs(int[] clientSiteIds)
        {
            return _context.ClientSitePocs
                .Where(z => clientSiteIds.Contains(z.ClientSiteId) && !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }

        public void SaveClientSitePoc(ClientSitePoc clientSitePoc)
        {
            if (clientSitePoc.Id == -1)
            {
                clientSitePoc.Id = 0;
                _context.ClientSitePocs.Add(clientSitePoc);
            }
            else
            {
                var dataToUpdate = _context.ClientSitePocs.SingleOrDefault(z => z.Id == clientSitePoc.Id);
                if (dataToUpdate != null)
                {
                    dataToUpdate.Name = clientSitePoc.Name;
                    dataToUpdate.Email = clientSitePoc.Email;
                }
            }

            _context.SaveChanges();
        }

        public void DeleteClientSitePoc(int id)
        {
            var clientSitePocToDelete = _context.ClientSitePocs.SingleOrDefault(x => x.Id == id);
            if (clientSitePocToDelete != null)
            {
                clientSitePocToDelete.IsDeleted = true;
                _context.SaveChanges();
            }            
        }

        public List<ClientSiteLocation> GetClientSiteLocations(int clientSiteId)
        {
            return _context.ClientSiteLocations
                .Where(z => z.ClientSiteId == clientSiteId && !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }

        public List<ClientSiteLocation> GetClientSiteLocations(int[] clientSiteIds)
        {
            return _context.ClientSiteLocations
                .Where(z => clientSiteIds.Contains(z.ClientSiteId) && !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }

        public void SaveClientSiteLocation(ClientSiteLocation clientSiteLocation)
        {
            if (clientSiteLocation.Id == -1)
            {
                clientSiteLocation.Id = 0;
                _context.ClientSiteLocations.Add(clientSiteLocation);
            }
            else
            {
                var dataToUpdate = _context.ClientSiteLocations.SingleOrDefault(z => z.Id == clientSiteLocation.Id);
                if (dataToUpdate != null)
                {
                    dataToUpdate.Name = clientSiteLocation.Name;
                }
            }

            _context.SaveChanges();
        }

        public void DeleteClientSiteLocation(int id)
        {
            var clientSiteLocationToDelete = _context.ClientSiteLocations.SingleOrDefault(x => x.Id == id);
            if (clientSiteLocationToDelete != null)
            {
                clientSiteLocationToDelete.IsDeleted = true;
                _context.SaveChanges();
            }
        }

        public List<ClientSiteKey> GetClientSiteKeys(int clientSiteId)
        {
            return _context.ClientSiteKeys
                .Where(z => z.ClientSiteId == clientSiteId && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(z => z.KeyNo)
                .ToList();
        }
        public ANPR GetANPRCheckbox(int clientSiteId)
        {
            return _context.ANPR
                .Where(z => z.ClientSiteId == clientSiteId)
                .Include(x => x.ClientSite)
                .FirstOrDefault();
        }
        public List<ANPR> GetANPR(int clientSiteId)
        {
            return _context.ANPR
                .Where(z => z.ClientSiteId == clientSiteId && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .ToList();
        }
        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds)
        {
            return _context.ClientSiteKeys
                .Where(z => clientSiteIds.Contains(z.ClientSiteId) && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(z => z.KeyNo)
                .ToList();
        }

        public void SaveClientSiteKey(ClientSiteKey clientSiteKey)
        {
            if (clientSiteKey == null)
                throw new ArgumentNullException();

            if (clientSiteKey.Id == 0)
            {
                _context.ClientSiteKeys.Add(clientSiteKey);
            }
            else
            {
                var clientSiteKeyToUpdate = _context.ClientSiteKeys.SingleOrDefault(x => x.Id == clientSiteKey.Id);
                if (clientSiteKeyToUpdate != null)
                {
                    clientSiteKeyToUpdate.KeyNo = clientSiteKey.KeyNo;
                    clientSiteKeyToUpdate.Description = clientSiteKey.Description;
                    //p2-140 key photos  -start
                    clientSiteKeyToUpdate.ImagePath = clientSiteKey.ImagePath;
                    //p2-140 key photos  -end
                }
            }
            _context.SaveChanges();
        }
        public void SaveANPR(ANPR anpr)
        {
            if (anpr == null)
                throw new ArgumentNullException();

            if (anpr.Id == 0)
            {
                _context.ANPR.Add(anpr);
            }
            else
            {
                var anprToUpdate = _context.ANPR.SingleOrDefault(x => x.Id == anpr.Id);
                if (anprToUpdate != null)
                {
                    anprToUpdate.profile = anpr.profile;
                    anprToUpdate.Apicalls = anpr.Apicalls;
                    anprToUpdate.LaneLabel = anpr.LaneLabel;
                    anprToUpdate.IsDisabled = anpr.IsDisabled;
                    anprToUpdate.IsSingleLane = anpr.IsSingleLane;
                    anprToUpdate.IsSeperateEntryAndExitLane = anpr.IsSeperateEntryAndExitLane;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteClientSiteKey(int id)
        {
            var clientSiteKeyToDelete = _context.ClientSiteKeys.SingleOrDefault(x => x.Id == id);
            if (clientSiteKeyToDelete != null)
            {
                _context.ClientSiteKeys.Remove(clientSiteKeyToDelete);
                _context.SaveChanges();
            }
        }
        //p2-140 key photos  -start

        public void DeleteANPR(int id)
        {
            var ANPRToDelete = _context.ANPR.SingleOrDefault(x => x.Id == id);
            if (ANPRToDelete != null)
            {
                _context.ANPR.Remove(ANPRToDelete);
                _context.SaveChanges();
            }
        }
        public void DeleteClientSiteKeyImage(int id)
        {
            var clientSiteKeyToDelete = _context.ClientSiteKeys.SingleOrDefault(x => x.Id == id);
            if (clientSiteKeyToDelete != null)
            {
                clientSiteKeyToDelete.ImagePath = string.Empty;
                
            }
            _context.SaveChanges();
        }
        //p2-140 key photos  -end
    }

}
