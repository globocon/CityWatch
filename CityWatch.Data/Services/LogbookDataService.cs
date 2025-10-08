using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Linq;

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

                //Check and create SmartWandLog if enabled for client site
                if (logBookType != LogBookType.SmartWandLog)
                {
                    var clientSite = _clientDataProvider.GetClientSiteDetails(clientSiteId);
                    if(clientSite != null && clientSite.IsActive && clientSite.UploadSWLog)
                    {
                        var swclientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.SmartWandLog, DateTime.Today);
                        if (swclientSiteLogBook == null)
                        {
                            var newSWClientSiteLogBook = new ClientSiteLogBook()
                            {
                                ClientSiteId = clientSiteId,
                                Type = LogBookType.SmartWandLog,
                                Date = DateTime.Today,
                                DbxUploaded = false
                            };
                            var newSWLogBookId = _clientDataProvider.SaveClientSiteLogBook(newSWClientSiteLogBook);
                        }
                    }
                }
            }

            return newLogBookId;
        }
    }
}
