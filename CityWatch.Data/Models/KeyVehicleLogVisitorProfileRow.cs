namespace CityWatch.Data.Models
{
    /// <summary>
    /// Flat projection of a visitor profile for the Audit Site Log "Visitor Profiles" grid.
    /// Only the columns the grid (and its expandable detail row) shows - the full entities carry
    /// POI images and the whole KeyVehicleLog, which made the unpaged list ~300 MB of JSON.
    /// </summary>
    public class KeyVehicleLogVisitorProfileRow
    {
        public int Id { get; set; }
        public string VehicleRego { get; set; }
        public int? PlateId { get; set; }
        public string PoiOrBdm { get; set; }
        public string CompanyName { get; set; }
        public string PersonName { get; set; }
        public int? PersonType { get; set; }
        public string ClientSite { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public int CreatedLogId { get; set; }
        public int? TruckConfig { get; set; }
        public int? TrailerType { get; set; }
        public string Trailer1Rego { get; set; }
        public string Trailer2Rego { get; set; }
        public string Trailer3Rego { get; set; }
        public string Trailer4Rego { get; set; }
        public string MobileNumber { get; set; }
        public string Product { get; set; }
        public int? EntryReason { get; set; }
        public string Notes { get; set; }

        // Lookup names, filled in from KeyVehicleLogFields after the query
        public string Plate { get; set; }
        public string TruckConfigText { get; set; }
        public string TrailerTypeText { get; set; }
        public string PersonTypeText { get; set; }
        public string PurposeOfEntry { get; set; }
    }
}
