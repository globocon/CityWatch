using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Linq;

namespace CityWatch.Data.Services
{
    public interface ILogbookDataService
    {
        int GetNewOrExistingClientSiteLogBookId(int clientSiteId, LogBookType logBookType);
        int GetNewOrExistingClientSiteLogBookId(int clientSiteId, LogBookType logBookType, DateTime LogBookDate);
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
            return GetOrCreateNewLogBook(clientSiteId, logBookType, DateTime.Today);
        }

        public int GetNewOrExistingClientSiteLogBookId(int clientSiteId, LogBookType logBookType, DateTime LogBookDate)
        {
            return GetOrCreateNewLogBook(clientSiteId, logBookType, LogBookDate);
        }

        public int GetOrCreateNewLogBook(int clientSiteId, LogBookType logBookType, DateTime LogBookDate)
        {
            int newLogBookId;
            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, logBookType, LogBookDate.Date);
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
                    Date = LogBookDate,
                    DbxUploaded = false
                };
                newLogBookId = _clientDataProvider.SaveClientSiteLogBook(newClientSiteLogBook);

                var clientSite = _clientDataProvider.GetClientSiteDetails(clientSiteId);
                //Check and create SmartWandLog if enabled for client site
                if (logBookType != LogBookType.SmartWandLog)
                {
                    if (clientSite != null && clientSite.IsActive && clientSite.UploadSWLog)
                    {
                        var swclientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.SmartWandLog, LogBookDate);
                        if (swclientSiteLogBook == null)
                        {
                            var newSWClientSiteLogBook = new ClientSiteLogBook()
                            {
                                ClientSiteId = clientSiteId,
                                Type = LogBookType.SmartWandLog,
                                Date = LogBookDate,
                                DbxUploaded = false
                            };
                            var newSWLogBookId = _clientDataProvider.SaveClientSiteLogBook(newSWClientSiteLogBook);
                        }
                    }
                }
                //Check and create FusionLog if enabled for client site
                if (logBookType != LogBookType.FusionLog)
                {
                    if (clientSite != null && clientSite.IsActive && clientSite.UploadFusionLog)
                    {
                        var fusionClientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.FusionLog, LogBookDate);
                        if (fusionClientSiteLogBook == null)
                        {
                            var newFusionClientSiteLogBook = new ClientSiteLogBook()
                            {
                                ClientSiteId = clientSiteId,
                                Type = LogBookType.FusionLog,
                                Date = LogBookDate,
                                DbxUploaded = false
                            };
                            var newFusionLogBookId = _clientDataProvider.SaveClientSiteLogBook(newFusionClientSiteLogBook);
                        }
                    }
                }
            }

            return newLogBookId;
        }
    }
}
