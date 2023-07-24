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

        public KeyVehicleLogProfileViewModel(KeyVehicleLogProfile keyVehicleLogProfile, List<KeyVehcileLogField> keyVehcileLogFields)
        {
            _keyVehicleLogProfile = keyVehicleLogProfile;
            _keyVehicleLogFields = keyVehcileLogFields;            
        }

        public KeyVehicleLogProfile Detail
        {
            get
            {
                return _keyVehicleLogProfile;
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
                return _keyVehicleLogFields.SingleOrDefault(z => z.Id == _keyVehicleLogProfile.PersonType)?.Name;
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
