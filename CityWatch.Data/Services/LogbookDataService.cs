using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;

namespace CityWatch.Data.Services
{
    public interface ILogbookDataService
    {
        int GetNewOrExistingClientSiteLogBookId(int clientSiteId, LogBookType logBookType);
    }

    public class LogbookDataService : ILogbookDataService
    {
        private readonly IClientDataProvider _clientDataProvider;

        public LogbookDataService(IClientDataProvider clientDataProvider)
        {
            _clientDataProvider = clientDataProvider;           
        }

        public int GetNewOrExistingClientSiteLogBookId(int clientSiteId, LogBookType logBookType)
        {
            int newLogBookId;
            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, logBookType, DateTime.Today);
            if (clientSiteLogBook != null)
            {
                newLogBookId = clientSiteLogBook.Id;
            }
            else
            {
                var newClientSiteLogBook = new ClientSiteLogBook()
                {
                    ClientSiteId = clientSiteId,
                    Type = logBookType,
                    Date = DateTime.Today,
                    DbxUploaded = false
                };
                newLogBookId = _clientDataProvider.SaveClientSiteLogBook(newClientSiteLogBook);
            }

            return newLogBookId;
        }
    }
}
