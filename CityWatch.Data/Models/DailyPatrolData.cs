using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Configuration;
using CityWatch.Data;
using Microsoft.AspNetCore.Hosting;
using System.Configuration;
using static System.Net.WebRequestMethods;
using Azure;


namespace CityWatch.Data.Models
{
    public class DailyPatrolData
    {
        private readonly IncidentReport _incidentReport;
        private readonly List<ClientSite> _clientSites;
        public readonly IConfigDataProvider _configDataProvider;
        
        public DailyPatrolData(IncidentReport incidentReport, List<ClientSite> clientSites, IConfigDataProvider configDataProvider)
        {
            _incidentReport = incidentReport;
            _clientSites = clientSites;
            _configDataProvider = configDataProvider;
            
        }

        public string NameOfDay
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.DayOfWeek.ToString() :
                    string.Empty;
            }
        }

        public string Date
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.ToString("dd MMM yyyy") :
                    string.Empty;
            }
        }

        public string ControlRoomJobNo
        {
            get
            {
                return _incidentReport.JobNumber;
            }
        }

        public string SiteName
        {
            get
            {
                var siteName = string.Empty;
                if (_incidentReport.ClientSiteId.HasValue)
                    siteName = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteId.Value)?.Name;
                return siteName;
            }
        }

        public string SiteAddress
        {
            get
            {
                var address = string.Empty;
                if (_incidentReport.ClientSiteId.HasValue)
                    address = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteId.Value)?.Address;
                return address;
            }
        }

        public string DespatchTime
        {
            get
            {
                return _incidentReport.JobTime ?? "n/a";
            }
        }

        public string ArrivalTime
        {
            get
            {
                return _incidentReport.IncidentDateTime.HasValue ?
                    _incidentReport.IncidentDateTime.Value.ToString("HH:mm") :
                    string.Empty;
            }
        }

        public string DepartureTime
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.ToString("HH:mm") :
                    string.Empty;
            }
        }

        public string SerialNo
        {
            get
            {
                return _incidentReport.SerialNo;
            }
        }

        public string TotalMinsOnsite
        {
            get
            {
                return _incidentReport.IncidentDateTime.HasValue && _incidentReport.ReportDateTime.HasValue ?
                    (_incidentReport.ReportDateTime.Value - _incidentReport.IncidentDateTime.Value).TotalMinutes.ToString() :
                    string.Empty;
            }
        }

        public string ResponseTime
        {
            get
            {
                if (!string.IsNullOrEmpty(_incidentReport.JobTime) && _incidentReport.IncidentDateTime.HasValue)
                {
                    var tsJob = TimeSpan.Parse(_incidentReport.JobTime);
                    var dtJob = new DateTime(_incidentReport.IncidentDateTime.Value.Year,
                        _incidentReport.IncidentDateTime.Value.Month, _incidentReport.IncidentDateTime.Value.Day,
                        tsJob.Hours, tsJob.Minutes, 0);
                    if (dtJob > _incidentReport.IncidentDateTime.Value)
                        dtJob = dtJob.AddDays(-1);

                    return (_incidentReport.IncidentDateTime.Value - dtJob).TotalMinutes.ToString();
                }
                return string.Empty;
            }
        }

        public string Alarm
        {
            get
            {
                var isFireOrAlarm = _incidentReport.IsEventFireOrAlarm ? "Yes" : string.Empty;
                return $"{isFireOrAlarm} {Environment.NewLine}{_incidentReport.ClientArea}";
            }
        }

        public string ClientArea
        {
            get
            {
                return _incidentReport.ClientArea;
            }
        }

        public string PatrolAttented
        {
            get
            {
                return _incidentReport.CallSign;
            }
        }

        public string ActionTaken
        {
            get
            {
                return _incidentReport.ActionTaken;
            }
        }

        public string NotifiedBy
        {
            get
            {
                return _incidentReport.NotifiedBy;
            }
        }

        public string Billing
        {
            get
            {
                return _incidentReport.Billing;
            }
        }

        public ICollection<IncidentReportEventType> IncidentReportEventTypes
        {
            get
            {
                return _incidentReport.IncidentReportEventTypes;
            }
        }

        public int? ColorCode
        {
            get
            {
                return _incidentReport.ColourCode;
            }
        }

        public string fileNametodownload {
            get
            {
                return _incidentReport.FileName;
            }

        }
        public string pspfname
        {
            get
            {
                var pspfid= _incidentReport.PSPFId.Value;
                return _configDataProvider.GetPSPFNameFromId(pspfid);              



            }

        }
        public async Task<long?> GetBlobSizeAsync()

        {
            try
            {
         
                string azureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=c4istorage1;AccountKey=12Q6IOpVChRVlh/gCuYdYoZfkWCDhV82f02Cu9md5jDbiAkxeHuyShvCXrIazkrZYuYj0QmY73ii+AStLR19UQ==;EndpointSuffix=core.windows.net";

                string containerName = "irfiles";
                string blobPath = _incidentReport.FileName.Substring(0, 8) + '/' + _incidentReport.FileName;
               
                BlobServiceClient blobServiceClient = new BlobServiceClient(azureStorageConnectionString);
                // Get the BlobContainerClient using the container name
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Get the BlobClient using the blob path
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobPath);

                if (blobClient != null)
                {
                    BlobProperties properties = await blobClient.GetPropertiesAsync();
                    return properties.ContentLength;
                    
             
                }
                else
                {
                    Console.WriteLine("File not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;

            }
        }

       

        public string hashvalue
        {
            get
            {
                return _incidentReport.HASH;
                
            }

        }
    }
}
