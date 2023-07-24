namespace CityWatch.Data.Models
{
    public class ClientSiteLocation
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsDeleted { get; set; }
    }
}
