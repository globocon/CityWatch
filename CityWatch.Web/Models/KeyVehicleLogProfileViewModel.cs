using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class KeyVehicleLogProfileViewModel
    {
        private readonly KeyVehicleLogProfile _keyVehicleLogProfile;
        private readonly List<KeyVehcileLogField> _keyVehicleLogFields;
        public readonly KeyVehicleLogVisitorPersonalDetail _vehicleLogVisitorPersonalDetail;

        public KeyVehicleLogProfileViewModel(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail,
            List<KeyVehcileLogField> keyVehicleLogFields)
        {
            _vehicleLogVisitorPersonalDetail = keyVehicleLogVisitorPersonalDetail;
            _keyVehicleLogProfile = keyVehicleLogVisitorPersonalDetail.KeyVehicleLogProfile;
            _keyVehicleLogFields = keyVehicleLogFields;
        }

        public KeyVehicleLogVisitorPersonalDetail Detail
        {
            get
            {
                return _vehicleLogVisitorPersonalDetail;
            }
        }

        public string TruckConfigText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogProfile.TruckConfig)?.Name;
            }
        }

        public string TrailerTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogProfile.TrailerType)?.Name;
            }
        }

        public string PersonTypeText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _vehicleLogVisitorPersonalDetail.PersonType)?.Name;
            }
        }

        public string EntryReasonText
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogProfile.EntryReason)?.Name;
            }
        }

        public string PurposeOfEntry
        {
            get
            {
                var entryPurposeOrProduct = _keyVehicleLogProfile.Product;

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

        public string Plate
        {
            get
            {
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogProfile.PlateId)?.Name;
            }
        }

        public string ClientSite
        {
            get
            {
                return _keyVehicleLogProfile.KeyVehicleLog?.ClientSiteLogBook.ClientSite.Name;
            }
        }

        public int? ClientSiteId
        {
            get
            {
                return _keyVehicleLogProfile.KeyVehicleLog?.ClientSiteLogBook.ClientSite.Id;
            }
        }
    }

    public class KeyVehicleLogProfileViewModelComparer : IComparer<KeyVehicleLogProfileViewModel>
    {
        public int Compare(KeyVehicleLogProfileViewModel x, KeyVehicleLogProfileViewModel y)
        {
            var result = string.Compare(x.Detail.CompanyName, y.Detail.CompanyName, StringComparison.OrdinalIgnoreCase);

            if (result == 0)
            {
                result = string.Compare(x.Detail.PersonName, y.Detail.PersonName, StringComparison.OrdinalIgnoreCase);
            }

            if (result == 0)
            {
                result = string.Compare(x.PersonTypeText, y.PersonTypeText, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }
    }
}
