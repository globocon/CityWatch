using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Web.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class KeyVehicleLogViewModel
    {
        private readonly KeyVehicleLog _keyVehicleLog;
        private readonly List<KeyVehcileLogField> _keyVehicleLogFields;
        private readonly List<KeyVehicleLog> _keyVehicleLoglist;
        private readonly List<KeyVehicleLogPax> _keyVehicleLogPaxlist;

        public KeyVehicleLogViewModel(List<KeyVehicleLog> keyVehicleLog, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLoglist = keyVehicleLog;
            _keyVehicleLogFields = keyVehcileLogFields;
        }
        //p7-137--pax-start
        public KeyVehicleLogViewModel(KeyVehicleLog keyVehicleLog, List<KeyVehcileLogField> keyVehcileLogFields, List<KeyVehicleLogPax> keyVehicleLogPax)
        {
            _keyVehicleLog = keyVehicleLog;
            _keyVehicleLogFields = keyVehcileLogFields;
            _keyVehicleLogPaxlist = keyVehicleLogPax;
        }
        //p7-137--pax-end
        public KeyVehicleLogViewModel(KeyVehicleLog keyVehicleLog, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLog = keyVehicleLog;
            _keyVehicleLogFields = keyVehcileLogFields;
        }

        public string GroupText { get { return _keyVehicleLog.EntryTime?.Date.ToString("dd MMM yyyy"); } }
        //p7-137--pax-start
        public List<KeyVehicleLogPax> PaxDetails
        {
            get
            {
                return _keyVehicleLogPaxlist.Where(x=> x.KeyVehicleLogId == _keyVehicleLog.Id).ToList();
            }
        }
        //p7-137--pax-end
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
                if (Detail.ExitTime.HasValue || (Detail.HasLoadVariation && Detail.EntryTime.HasValue))
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
        public string Plate5
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer5PlateId)?.Name;
            }
        }
        public string Plate6
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer6PlateId)?.Name;
            }
        }
        public string Plate7
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer7PlateId)?.Name;
            }
        }
        public string Plate8
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLog.Trailer8PlateId)?.Name;
            }
        }
        //p7-137--pax-start
        public int PAX
        {
            get
            {
                return _keyVehicleLogPaxlist?
                    .Count(z => z.KeyVehicleLogId == _keyVehicleLog?.Id) ?? 0;
            }
        }
        //p7-137--pax-end

        public string VehicleRegoHeading
        {
            get
            {
                if (Detail.IsCarsStock.HasValue && Detail.IsCarsStock.Value)
                    return "Cars (Stock)";
                else if (Detail.IsISO.HasValue && Detail.IsISO.Value)
                    return "ISO No + Seal";
                else if (Detail.IsVin.HasValue && Detail.IsVin.Value)
                    return "VIN No + Seal";
                else if (Detail.IsTrailerRego.HasValue && Detail.IsTrailerRego.Value)
                    return "Trailer Rego";
                else
                    return "";
            }
        }

    }
}
