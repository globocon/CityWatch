using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public enum HrSettingsType
    {
        [Display(Name = "HR Groups")]
        HRGroups = 1,
        [Display(Name = "Licence Types")]
        LicenceTypes = 2,
        [Display(Name = "Critical Documents")]
        CriticalDocuments = 3,
        [Display(Name = "Settings")]
        Settings = 4,
    }
    public enum HrCriticalType
    {
        [Display(Name = "Critical Documents")]
        CriticalDocuments = 1,
    }
    public class HRGroups
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    public class ReferenceNoNumbers
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class ReferenceNoAlphabets
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    public class HrSettings
    {
        [Key]
        public int Id { get; set; }
        public int HRGroupId { get; set; }
        [ForeignKey("HRGroupId")]
        public HRGroups HRGroups { get; set; }

        public int ReferenceNoNumberId { get; set; }
        [ForeignKey("ReferenceNoNumberId")]
        public ReferenceNoNumbers ReferenceNoNumbers { get; set; }

        public int ReferenceNoAlphabetId { get; set; }
        [ForeignKey("ReferenceNoAlphabetId")]
        public ReferenceNoAlphabets ReferenceNoAlphabets { get; set; }

        public string Description { get; set; }
        [NotMapped]
        public string ReferenceNo { get { return ReferenceNoNumbers.Name + ReferenceNoAlphabets.Name  ; } }
        [NotMapped]
        public string GroupName { get { return HRGroups.Name ; } }

        public ICollection<HrSettingsClientSites> hrSettingsClientSites { get; set; }

        public ICollection<HrSettingsClientStates> hrSettingsClientStates { get; set; }
    }
}
