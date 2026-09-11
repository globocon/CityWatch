using CityWatch.Data.Models;
using CityWatch.Web.Services;
using System.Collections.Generic;
using System.Linq;
using CityWatch.Data.Providers;
using System;
namespace CityWatch.Web.Models
{
    public class KeyVehicleLogDocketViewModel
    {
        private readonly KeyVehicleLogDocketHistory _keyVehicleLogDocketHistory;
        private readonly List<KeyVehcileLogField> _keyVehicleLogFields;
        private readonly List<KeyVehicleLogDocketHistory> _keyVehicleLogDocketHistorylist;
        public KeyVehicleLogDocketViewModel(List<KeyVehicleLogDocketHistory> keyVehicleLogDocketHistorylist, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLogDocketHistorylist = keyVehicleLogDocketHistorylist;
            _keyVehicleLogFields = keyVehcileLogFields;
        }

        public KeyVehicleLogDocketViewModel(KeyVehicleLogDocketHistory keyVehicleLogDocketHistory, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLogDocketHistory = keyVehicleLogDocketHistory;
            _keyVehicleLogFields = keyVehcileLogFields;
        }
        public KeyVehicleLogDocketHistory Detail
        {
            get
            {
                return _keyVehicleLogDocketHistory;
            }
        }

        public KvlStatusFilter Status
        {
            get
            {
                if (Detail.KeyVehicleLog.ExitTime.HasValue || Detail.KeyVehicleLog.HasLoadVariation)
                    return KvlStatusFilter.Closed;

                if (Detail.KeyVehicleLog.EntryTime.HasValue)
                    return KvlStatusFilter.Open;

                return KvlStatusFilter.Pending;
            }
        }

        public string TruckConfigText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.TruckConfig)?.Name;
            }
        }

        public string TrailerTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.TrailerType)?.Name;
            }
        }

        public string PersonTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.PersonType)?.Name;
            }
        }

        public string EntryReasonText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.EntryReason)?.Name;
            }
        }

        public string PurposeOfEntry
        {
            get
            {
                var entryPurposeOrProduct = Detail.KeyVehicleLog.Product;

                if (!string.IsNullOrEmpty(entryPurposeOrProduct) && !string.IsNullOrEmpty(EntryReasonText))
                {
                    return entryPurposeOrProduct + ", " + EntryReasonText;
                }

                if (!string.IsNullOrEmpty(entryPurposeOrProduct) && string.IsNullOrEmpty(EntryReasonText))
                {
                    return entryPurposeOrProduct;
                }

                if (string.IsNullOrEmpty(entryPurposeOrProduct) && !string.IsNullOrEmpty(EntryReasonText))
                {
                    return EntryReasonText;
                }

                return string.Empty;
            }
        }

        public string ClientSiteLocationName
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.ClientSiteLocation?.Name;
            }
        }

        public string ClientSitePocName
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.ClientSitePoc?.Name;
            }
        }

        public string Plate
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.PlateId)?.Name;
            }
        }

        public string Plate1
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.Trailer1PlateId)?.Name;
            }
        }
        public string Plate2
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.Trailer2PlateId)?.Name;
            }
        }
        public string Plate3
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.Trailer3PlateId)?.Name;
            }
        }
        public string Plate4
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogDocketHistory.KeyVehicleLog.Trailer4PlateId)?.Name;
            }
        }
        public string ComplianceDocuments
        {
            get
            {
                if (_keyVehicleLogDocketHistory.KeyVehicleLog.IsDocketNo==true)
                {
                    return "Y";
                }
                return "NA";
            }
        }
        public string DateOfLog
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.ClientSiteLogBook.Date.ToString("yyyy-MMM-dd").ToUpper();
            }
        }
        public string IntialCall
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.InitialCallTime?.ToString("HH:mm");
            }
        }
        public string EntryTime
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.EntryTime?.ToString("HH:mm");
            }
        }
        public string SentInTime
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.SentInTime?.ToString("HH:mm");
            }
        }
        public string ExitTime
        {
            get
            {
                return _keyVehicleLogDocketHistory.KeyVehicleLog.ExitTime?.ToString("HH:mm");
            }
        }
        
        
    }

    public class KVLogDocketsViewModel
    {
        public int Id { get; set; }
        public int KvLogId { get; set; }
        public string FileNametodownload { get; set; }
        public string DateOfLog { get; set; }
        public string DocketSerialNo { get; set; }
        public string VehicleRego { get; set; }
        public string Plate { get; set; }
        public string TruckConfigText { get; set; }
        public string DocketReason { get; set; }
        public string PurposeOfEntry { get; set; }
        public string IntialCall { get; set; }
        public string EntryTime { get; set; }
        public string SentInTime { get; set; }
        public string ExitTime { get; set; }
    }
}
