using Microsoft.VisualBasic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWandTagsHitLog
    {
        [Key]
        public int Id { get; set; }
        public int LoggedInClientSiteId { get; set; }
        public int LoggedInUserId { get; set; }
        public int LoggedInGuardId { get; set; }
        public string TagUId { get; set; }
        public int? TagsTypeId { get; set; }
        public string LabelDescription { get; set; }
        public int? TagLinkedClientSiteId { get; set; }
        public DateTime HitUtcDateTime { get; set; } =  DateTime.UtcNow;


        [ForeignKey("LoggedInClientSiteId")]
        public ClientSite LoggedInClientSite { get; set; }
        [ForeignKey("TagLinkedClientSiteId")]
        public ClientSite LinkedClientSite { get; set; }
        [ForeignKey("TagsTypeId")]
        public SmartWandTagsType SmartWandTagsType { get; set; }                
    }
}
