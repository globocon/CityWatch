using CityWatch.Data.Models;
using CityWatch.Web.Services;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class KeyVehicleLogViewModel
    {
        private readonly KeyVehicleLog _keyVehicleLog;
        private readonly List<KeyVehcileLogField> _keyVehicleLogFields;
        private readonly List<KeyVehicleLog> _keyVehicleLoglist;
       
        public KeyVehicleLogViewModel(List<KeyVehicleLog> keyVehicleLog, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLoglist = keyVehicleLog;
            _keyVehicleLogFields = keyVehcileLogFields;
        }

        public KeyVehicleLogViewModel(KeyVehicleLog keyVehicleLog, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLog = keyVehicleLog;
            _keyVehicleLogFields = keyVehcileLogFields;
        }

        public string GroupText { get { return _keyVehicleLog.EntryTime?.Date.ToString("dd MMM yyyy"); } }

        
        public KeyVehicleLog Detail
        {
            get
            {
                return _keyVehicleLog;
            }
        }

        public KvlStatusFilter Status
        {
            get
            {
                if (Detail.ExitTime.HasValue)
                    return KvlStatusFilter.Closed;

                if (Detail.EntryTime.HasValue)
                    return KvlStatusFilter.Open;

                return KvlStatusFilter.Pending;
            }
        }

        public string TruckConfigText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.TruckConfig)?.Name;
            }
        }

        public string TrailerTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.TrailerType)?.Name;
            }
        }

        public string PersonTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.PersonType)?.Name;
            }
        }

        public string EntryReasonText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.EntryReason)?.Name;
            }
        }

        public string PurposeOfEntry
        {
            get
            {
                var entryPurposeOrProduct = Detail.Product;

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
                return _keyVehicleLog.ClientSiteLocation?.Name;
            }
        }

        public string ClientSitePocName
        {
            get
            {
                return _keyVehicleLog.ClientSitePoc?.Name;
            }
        }

        public string Plate
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.PlateId)?.Name;
            }
        }

        public string Plate1
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer1PlateId)?.Name;
            }
        }
        public string Plate2
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer2PlateId)?.Name;
            }
        }
        public string Plate3
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer3PlateId)?.Name;
            }
        }
        public string Plate4
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer4PlateId)?.Name;
            }
        }
    }
}
