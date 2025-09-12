using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteWandDataProvider
    {
        List<ClientSiteSmartWand> GetClientSiteSmartWands();
        List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms);
        void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        void DeleteClientSiteSmartWand(int id);
        List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId);
        void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar);
        void DeleteClientSitePatrolCar(int id);
        ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id);
        void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag);
        void DeleteClientSiteSmartWandTags(int id);
        List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags();
        List<SmartWandTagsType> GetSmartWandTagsType();
        void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log);

        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds);
        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds);
    }

    public class ClientSiteWandDataProvider : IClientSiteWandDataProvider
    {
        private readonly CityWatchDbContext _dbContext;
        private readonly IConfigDataProvider _configDataProvider;

        public ClientSiteWandDataProvider(CityWatchDbContext dbContext, IConfigDataProvider configDataProvider)
        {
            _dbContext = dbContext;
            _configDataProvider = configDataProvider;
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands()
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .ToList();
        }
        public ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id)
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .Where(x => x.PhoneNumber == PhoneNumber && x.Id != id)
                .FirstOrDefault();
        }
        public List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms)
        {
            return _dbContext.ClientSiteSmartWands
                .Include(x => x.ClientSite)
                .Where(x => (string.IsNullOrEmpty(searchTerms) || x.PhoneNumber.Contains(searchTerms)) && x.ClientSite.IsActive == true && x.IsDeleted == false)
                .ToList();
        }

        public void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id == -1)
            {
                clientSiteSmartWand.Id = 0;
                clientSiteSmartWand.IsDeleted = false;
                _dbContext.ClientSiteSmartWands.Add(clientSiteSmartWand);
            }
            else
            {
                var clientSiteSmartWandToUpdate = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == clientSiteSmartWand.Id);
                if (clientSiteSmartWandToUpdate != null)
                {
                    clientSiteSmartWandToUpdate.SmartWandId = clientSiteSmartWand.SmartWandId;
                    clientSiteSmartWandToUpdate.PhoneNumber = clientSiteSmartWand.PhoneNumber;
                    clientSiteSmartWandToUpdate.SIMProvider = clientSiteSmartWand.SIMProvider;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSiteSmartWand(int id)
        {
            var deleteClientSiteSmartWand = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteSmartWand != null)
            {
                // Mark as deleted instead of removing it from the database
                deleteClientSiteSmartWand.IsDeleted = true;
            }
            else
            {
                throw new ArgumentException($"No Smart Wand found with ID: {id}");
            }

            _dbContext.SaveChanges();
        }

        public List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId)
        {
            return _dbContext.ClientSitePatrolCars
                .Where(x => x.ClientSiteId == clientSiteId && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .ToList();
        }

        public void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar)
        {
            if (clientSitePatrolCar.Id == -1)
            {
                clientSitePatrolCar.Id = 0;
                _dbContext.ClientSitePatrolCars.Add(clientSitePatrolCar);
            }
            else
            {
                var clientSitePatrolCarToUpdate = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == clientSitePatrolCar.Id);
                if (clientSitePatrolCarToUpdate != null)
                {
                    clientSitePatrolCarToUpdate.Model = clientSitePatrolCar.Model;
                    clientSitePatrolCarToUpdate.Rego = clientSitePatrolCar.Rego;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSitePatrolCar(int id)
        {
            var clientSitePatrolCarToDelete = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == id);
            if (clientSitePatrolCarToDelete != null)
            {
                _dbContext.ClientSitePatrolCars.Remove(clientSitePatrolCarToDelete);
                _dbContext.SaveChanges();
            }
        }
        public List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags()
        {
            var smartwandtags = _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite)
                .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }
        public List<SmartWandTagsType> GetSmartWandTagsType()
        {
            var smartwandtags = _configDataProvider.GetSmartWandTagsType();
            return smartwandtags;
        }

        public void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log)
        {
            if (log == null)
                throw new ArgumentNullException();
            
            _dbContext.ClientSiteSmartWandTagsHitLogs.Add(log);
            _dbContext.SaveChanges();
        }

        public void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag)
        {
            if (clientSiteSmartWandTag == null)
                throw new ArgumentNullException();

            if(clientSiteSmartWandTag.TagsType == null)
                throw new ArgumentNullException("TagsType cannot be null. Please select a tag type.");

            clientSiteSmartWandTag.TagsTypeId = _dbContext.SmartWandTagsType.Where(x => x.value == clientSiteSmartWandTag.TagsType).FirstOrDefault().Id;
            var _existingTagUID = _dbContext.ClientSiteSmartWandTags.Where(x => x.UId == clientSiteSmartWandTag.UId
                && x.ClientSite.IsActive == true && x.IsDeleted == false).ToList();

            if (clientSiteSmartWandTag.Id == -1)
            {
                clientSiteSmartWandTag.Id = 0;

                if (_existingTagUID.Any() || _existingTagUID.Count > 0)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                _dbContext.ClientSiteSmartWandTags.Add(clientSiteSmartWandTag);
            }
            else
            {
                bool isTagExists = _dbContext.ClientSiteSmartWandTags.Any(x => x.UId == clientSiteSmartWandTag.UId
                        && x.Id != clientSiteSmartWandTag.Id && x.ClientSite.IsActive == true && x.IsDeleted == false);
                if (isTagExists)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                var clientSiteSmartWandTagToUpdate = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == clientSiteSmartWandTag.Id);
                if (clientSiteSmartWandTagToUpdate != null)
                {
                    clientSiteSmartWandTagToUpdate.UId = clientSiteSmartWandTag.UId;
                    clientSiteSmartWandTagToUpdate.ClientSiteId = clientSiteSmartWandTag.ClientSiteId;
                    clientSiteSmartWandTagToUpdate.TagsTypeId = clientSiteSmartWandTag.TagsTypeId;
                    clientSiteSmartWandTagToUpdate.LabelDescription = clientSiteSmartWandTag.LabelDescription;
                    clientSiteSmartWandTagToUpdate.IsDeleted = false;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSiteSmartWandTags(int id)
        {
            var deleteClientSiteSmartWandTags = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteSmartWandTags != null)
                deleteClientSiteSmartWandTags.IsDeleted = true;
            //_dbContext.ClientSiteSmartWandTags.Remove(deleteClientSiteSmartWandTags);

            _dbContext.SaveChanges();
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds)
        {
            return _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive && !x.IsDeleted && clientSiteIds.Contains(x.ClientSiteId))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite)
                .AsEnumerable() // switch to in-memory to allow property set
                .Select(x =>
                {
                    x.TagsType = x.SmartWandTagsType.value;
                    return x;
                })
                .ToList();
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds)
        {
            var logs =  _dbContext.ClientSiteSmartWandTagsHitLogs
                .Where(x => clientSiteIds.Contains(x.LoggedInClientSiteId) ||
                            (x.TagLinkedClientSiteId.HasValue && clientSiteIds.Contains(x.TagLinkedClientSiteId.Value)))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.LoggedInClientSite)
                .Include(x => x.LinkedClientSite)
                .ToList();


            var tagList = logs.Select(x => new ClientSiteSmartWandTags
            {
                // Use TagLinkedClientSiteId if available, otherwise fall back to LoggedInClientSiteId
                ClientSiteId = x.TagLinkedClientSiteId ?? x.LoggedInClientSiteId,
                UId = x.TagUId,
                TagsTypeId = x.TagsTypeId ?? 0, // or handle null case as needed
                LabelDescription = x.LabelDescription,
                SmartWandTagsType = x.SmartWandTagsType,
                TagsType = x.SmartWandTagsType?.value,
                ClientSite = x.TagLinkedClientSiteId.HasValue ? x.LinkedClientSite : x.LoggedInClientSite,
                IsDeleted = false // assuming all from logs are "not deleted"
            })
            .GroupBy(x => new { x.ClientSiteId, x.UId }) // Optional: remove duplicates
            .Select(g => g.First()) // Keep one per UID per site
            .ToList();

            return tagList;
        }
    }
}
