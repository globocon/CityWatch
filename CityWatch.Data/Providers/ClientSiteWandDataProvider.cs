using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteWandDataProvider
    {
        List<ClientSiteSmartWand> GetClientSiteSmartWands();
        void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        void DeleteClientSiteSmartWand(int id);
        List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId);
        void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar);
        void DeleteClientSitePatrolCar(int id);
    }

    public class ClientSiteWandDataProvider : IClientSiteWandDataProvider
    {
        private readonly CityWatchDbContext _dbContext;

        public ClientSiteWandDataProvider(CityWatchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands()
        {
            return _dbContext.ClientSiteSmartWands
                .Include(x => x.ClientSite)
                .ToList();
        }

        public void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id == -1)
            {
                clientSiteSmartWand.Id = 0;
                _dbContext.ClientSiteSmartWands.Add(clientSiteSmartWand);
            }
            else
            {
                var clientSiteSmartWandToUpdate = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == clientSiteSmartWand.Id);
                if (clientSiteSmartWandToUpdate != null)
                {
                    clientSiteSmartWandToUpdate.SmartWandId = clientSiteSmartWand.SmartWandId;
                    clientSiteSmartWandToUpdate.PhoneNumber = clientSiteSmartWand.PhoneNumber;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSiteSmartWand(int id)
        {
            var deleteClientSiteSmartWand = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == id);
            if(deleteClientSiteSmartWand != null)
                _dbContext.ClientSiteSmartWands.Remove(deleteClientSiteSmartWand);
            
            _dbContext.SaveChanges();
        }

        public List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId)
        {
            return _dbContext.ClientSitePatrolCars
                .Where(x => x.ClientSiteId == clientSiteId)
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
    }   
}
