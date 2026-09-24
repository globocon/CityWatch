using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class GuardLogsLinked
    {
        [Key]
        public int Id { get; set; }
        public int GuardLogId { get; set; }
        public int LinkedGuardLogId { get; set; }
    }
}