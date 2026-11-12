using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class HrDoumentViewModel
    {
        public int Id { get; set; }
           
        public int[] ClientSiteIds { get; set; }

        public string ClientTypes { get; set; }

        public string ClientSites { get; set; }

        public string States { get; set; }
        public string GroupName { get; set; }
        public string referenceNo { get; set; }
        public string referenceNoAlphabetsName { get; set; }

        public string description { get; set; }

        public int referenceNoAlphabetId { get; set; }
        public int referenceNoNumberId { get; set; }
        public int hrGroupId { get; set; }
        public string ClientSitesSummary { get; set; }

        public bool hrlock { get; set; }
        public bool hrbanedit { get; set; }
        public string CourseColour { get; set; }
        public HrDateType DateType { get; set; }
        public string DateTypeName { get; set; }



        public static HrDoumentViewModel FromDataModel(HrSettings hrSettings)
        {
            var _clientsites = hrSettings.hrSettingsClientSites != null ? string.Join("<br>", hrSettings.hrSettingsClientSites.Select(z => z.ClientSite.Name).Distinct()) : string.Empty;
            var _clientStates = hrSettings.hrSettingsClientStates != null ? string.Join(", ", hrSettings.hrSettingsClientStates.Select(z => z.State.Trim()).Distinct()) : string.Empty;
            if (hrSettings.IsAllClientTypeEnabled) {
                _clientsites = "<strong>Select All <span class=\"text-danger\">Mandatory</span></strong>";
            }
            if (hrSettings.IsAllStateEnabled)
            {
                _clientStates = "<strong>Select All <span class=\"text-danger\">Mandatory</span></strong>";
            }
            return new HrDoumentViewModel()
            {
                Id = hrSettings.Id,                
                ClientTypes = hrSettings.hrSettingsClientSites!=null? string.Join(", ", hrSettings.hrSettingsClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()):string.Empty,
                ClientSites = _clientsites,
                States = _clientStates,
                GroupName = hrSettings.GroupName,
                referenceNo = hrSettings.ReferenceNo,
                description= hrSettings.Description,
                referenceNoAlphabetsName= hrSettings.ReferenceNoAlphabets.Name,
                referenceNoNumberId = hrSettings.ReferenceNoNumberId,
                referenceNoAlphabetId = hrSettings.ReferenceNoAlphabetId,
                hrGroupId = hrSettings.HRGroupId,
                ClientSitesSummary = hrSettings.hrSettingsClientSites != null ? GetFormattedClientSites(hrSettings.hrSettingsClientSites, hrSettings.IsAllClientTypeEnabled) : string.Empty,
                hrlock= hrSettings.HRLock,
                hrbanedit=hrSettings.HRBanEdit,
                CourseColour = hrSettings.CourseColour,
                DateType = hrSettings.DateType,
                DateTypeName = GetDateTypeName(hrSettings.DateType)
            };
        }

        private static string GetDateTypeName(HrDateType dateType)
        {
            switch (dateType)
            {
                case HrDateType.DOI:
                    return "DOI";
                case HrDateType.DOE:
                    return "DOE";
                case HrDateType.Both:
                default:
                    return "DOI / DOE";
            }
        }

        private static string GetFormattedClientSites(IEnumerable<HrSettingsClientSites> hrSettingsClientSites,bool isAllClientTypeEnabled = false)
        {
            var clientSites = hrSettingsClientSites.Select(x => x.ClientSite.Name).OrderBy(x => x);

            if (isAllClientTypeEnabled)
            {
                return "<strong>Select All <span class=\"text-danger\">Mandatory</span></strong>";
            }
            if (clientSites.Count() == 0)
                return "";
            if (clientSites.Count() <= 2)
                return string.Join("<br>", clientSites);

            return $"{string.Join("<br>", clientSites.Take(2))} <br><span class=\"text-primary\"> and {clientSites.Count() - 2} more sites </span>";
        }
    }

   

    
}
