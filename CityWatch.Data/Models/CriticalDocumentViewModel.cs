using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class CriticalDocumentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Sites")]
        public int[] ClientSiteIds { get; set; }
        [Required]
        [Display(Name = "Description")]
        public int[] DescriptionIds { get; set; }
        public int ClientTypeId { get; set; }
        public string ClientTypes { get; set; }
        public string ClientSites { get; set; }
        public string Descriptions { get; set; }
        public int HRGroupID { get; set; }
        public string HRGroupName { get; set; }
        public string ReferenceNO { get; set; }
        public static CriticalDocuments ToDataModel(CriticalDocumentViewModel viewModel)
        {
            int maxLength = Math.Max(viewModel.ClientSiteIds.Length, viewModel.DescriptionIds.Length);

            var criticalDocumentsClientSites = new List<CriticalDocumentsClientSites>();

            for (int i = 0; i < maxLength; i++)
            {
                
                int clientId = i < viewModel.ClientSiteIds.Length ? viewModel.ClientSiteIds[i] : default(int);
                int descriptionId = i < viewModel.DescriptionIds.Length ? viewModel.DescriptionIds[i] : default(int);

                criticalDocumentsClientSites.Add(new CriticalDocumentsClientSites()
                {
                    CriticalDocumentID = viewModel.Id,
                    ClientSiteId = clientId,
                    DescriptionID = descriptionId
                });
            }

            return new CriticalDocuments()
            {
                Id = viewModel.Id,
                HRGroupID = viewModel.HRGroupID,
                ClientTypeId = viewModel.ClientTypeId,
                CriticalDocumentsClientSites = criticalDocumentsClientSites
            };
        }
        public static CriticalDocumentViewModel FromDataModel(CriticalDocuments dataModel)
        {
            return new CriticalDocumentViewModel()
            {
                Id = dataModel.Id,
                ClientSiteIds = dataModel.CriticalDocumentsClientSites.Select(z => z.ClientSiteId).ToArray(),
                ClientTypes = string.Join(", ", dataModel.CriticalDocumentsClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()),
                ClientSites = string.Join(", ", dataModel.CriticalDocumentsClientSites.Select(z => z.ClientSite.Name).Distinct()),
                Descriptions =string.Join(", ", dataModel.CriticalDocumentsClientSites.Select(z => z.HRSettings.Description).Distinct()),
                HRGroupName = string.Join(", ", dataModel.CriticalDocumentsClientSites.Select(z => z.HRSettings.HRGroups.Name).Distinct()),
                ReferenceNO= string.Join(", ", dataModel.CriticalDocumentsClientSites.Select(z => z.HRSettings.ReferenceNo).Distinct()),
            };
        }

    }
}
