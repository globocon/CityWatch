using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /* Category lookup for staff documents (eg Training / Fire Training, General Multimedia / Client Multimedia).
       DocumentType matches StaffDocument.DocumentType (2=Training, 7=Multimedia) */
    public class StaffDocumentCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int DocumentType { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
