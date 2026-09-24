using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWandTags
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public string UId { get; set; }
        public int TagsTypeId { get; set; }

        public string LabelDescription { get; set; }
        public bool FqBypass { get; set; }


        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        [ForeignKey("TagsTypeId")]
        public SmartWandTagsType SmartWandTagsType { get; set; }

        [NotMapped]
        public string TagsType { get; set; }
        public bool IsDeleted { get; set; }
    }
}
