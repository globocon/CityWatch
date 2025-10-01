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
using System.Drawing.Imaging;

namespace CityWatch.Data.Models
{
    public  class DailyKeyvehicleLog
    {
        private readonly KeyVehicleLog _incidentReport;
        private readonly List<ClientSite> _clientSites;
        

        public DailyKeyvehicleLog(KeyVehicleLog incidentReport, List<ClientSite> clientSites)
        {
            _incidentReport = incidentReport;
            _clientSites = clientSites;
           

        }

        public string NameOfDay
        {
            get
            {
                return
                    _incidentReport.ClientSiteLogBook.Date.DayOfWeek.ToString();
            }
        }

        public string Date
        {
            get
            {
                return 
                    _incidentReport.ClientSiteLogBook.Date.ToString("dd MMM yyyy") ;
            }
        }

        public string ControlRoomJobNo
        {
            get;
            
        }

        public string SiteName
        {
            get
            {
                var siteName = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteLogBook.ClientSiteId)?.Name;
                return siteName;
            }
        }

        public string SiteAddress
        {
            get
            {
                var address = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteLogBook.ClientSiteId)?.Address;
                return address;
            }
        }

        public string DespatchTime
        {
            get
            {
                return  "n/a";
            }
        }

        public string ArrivalTime
        {
            get
            {
                return _incidentReport.EntryTime.HasValue ?
                    _incidentReport.EntryTime.Value.ToString("HH:mm") :
                    string.Empty;

            }
        }

        public string DepartureTime
        {
            get
            {
                return _incidentReport.ExitTime.HasValue ?
                   _incidentReport.ExitTime.Value.ToString("HH:mm") :
                   string.Empty;
            }
        }

        public string SerialNo
        {
            get
            {
                return _incidentReport.DocketSerialNo;
            }
        }

        public string TotalMinsOnsite
        {
            get
            {
                
                return _incidentReport.EntryTime.HasValue && _incidentReport.ExitTime.HasValue ?
                   (_incidentReport.ExitTime.Value - _incidentReport.EntryTime.Value).TotalMinutes.ToString() :
                   string.Empty;
            }
        }



        public string ResponseTime
        {
            get
            {
                

                return string.Empty;
            }
        }

        //public string ResponseTime
        //{
        //    get
        //    {
        //        if (!string.IsNullOrEmpty(_incidentReport.JobTime) && _incidentReport.IncidentDateTime.HasValue)
        //        {
        //            var tsJob = TimeSpan.Parse(_incidentReport.JobTime);
        //            var dtJob = new DateTime(_incidentReport.IncidentDateTime.Value.Year,
        //                _incidentReport.IncidentDateTime.Value.Month, _incidentReport.IncidentDateTime.Value.Day,
        //                tsJob.Hours, tsJob.Minutes, 0);
        //            if (dtJob > _incidentReport.IncidentDateTime.Value)
        //                dtJob = dtJob.AddDays(-1);

        //            return (_incidentReport.IncidentDateTime.Value - dtJob).TotalMinutes.ToString();
        //        }
        //        return string.Empty;
        //    }
        //}

        public string Alarm
        {
            get
            {
                var isFireOrAlarm = string.Empty;
                return isFireOrAlarm;
            }
        }

        public string ClientArea
        {
            get
            {
                return string.Empty ;
            }
        }

        public string PatrolAttented
        {
            get
            {
                return string.Empty;
                
            }
        }

        public string ActionTaken
        {
            get
            {
               return string.Empty;

            }
        }

        public string NotifiedBy
        {
            get
            {
                return string.Empty;

            }
        }

        public string Billing
        {
            get
            {
                return string.Empty;

            }
        }

       

        public int? ColorCode
        {
            get
            {

                return 0;


            }
        }
        public string ColorCodeStr
        {
            get
            {

                

                return string.Empty;
            }
        }
        public string fileNametodownload
        {
            get
            {
                return _incidentReport.ClientSiteLogBook.FileName;
            }

        }
        public string pspfname
        {
            get;

        }
       


    }
}
