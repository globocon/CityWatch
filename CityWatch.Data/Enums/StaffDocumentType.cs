using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    /* Values already stored in StaffDocuments.DocumentType - do not renumber */
    public enum StaffDocumentType
    {
        [Display(Name = "Company SOP")]
        CompanySop = 1,

        [Display(Name = "Training")]
        Training = 2,

        [Display(Name = "Templates & Forms")]
        TemplatesAndForms = 3,

        [Display(Name = "Client (Site) SOP")]
        ClientSiteSop = 4,

        [Display(Name = "Client (Alarm) SOP")]
        ClientAlarmSop = 6,

        [Display(Name = "Multimedia")]
        Multimedia = 7
    }
}
