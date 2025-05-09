using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    [Table("KeyVehicleLogVisitorProfiles")]
    public class KeyVehicleLogProfile
    {
        public KeyVehicleLogProfile()
        { }

        public KeyVehicleLogProfile(KeyVehicleLog keyVehicleLog)
        {
            VehicleRego = keyVehicleLog.VehicleRego;
            Trailer1Rego = keyVehicleLog.Trailer1Rego;
            Trailer2Rego = keyVehicleLog.Trailer2Rego;
            Trailer3Rego = keyVehicleLog.Trailer3Rego;
            Trailer4Rego = keyVehicleLog.Trailer4Rego;
            PlateId = keyVehicleLog.PlateId;            
            MobileNumber = keyVehicleLog.MobileNumber;
            Product = keyVehicleLog.Product;
            TruckConfig = keyVehicleLog.TruckConfig;
            TrailerType = keyVehicleLog.TrailerType;
            MaxWeight = keyVehicleLog.MaxWeight;
            EntryReason = keyVehicleLog.EntryReason;
            IsSender = keyVehicleLog.IsSender;
            Sender = keyVehicleLog.Sender;
            Trailer1PlateId = keyVehicleLog.Trailer1PlateId;
            Trailer2PlateId = keyVehicleLog.Trailer2PlateId;
            Trailer3PlateId = keyVehicleLog.Trailer3PlateId;
            Trailer4PlateId = keyVehicleLog.Trailer4PlateId;
        }

        [Key]
        public int Id { get; set; }

        public string VehicleRego { get; set; }

        public string Trailer1Rego { get; set; }

        public string Trailer2Rego { get; set; }

        public string Trailer3Rego { get; set; }

        public string Trailer4Rego { get; set; }

        public int? PlateId { get; set; }

        public int? TruckConfig { get; set; }

        public int? TrailerType { get; set; }

        public decimal? MaxWeight { get; set; }

        public string MobileNumber { get; set; }

        public string Product { get; set; }

        public int? EntryReason { get; set; }

        public int CreatedLogId { get; set; }

        [HiddenInput]
        public bool IsSender { get; set; }

        public string Sender { get; set; }

        [ForeignKey("CreatedLogId")]
        public KeyVehicleLog KeyVehicleLog { get; set; }

        public string Notes { get; set; }

        public int? Trailer1PlateId { get; set; }

        public int? Trailer2PlateId { get; set; }

        public int? Trailer3PlateId { get; set; }

        public int? Trailer4PlateId { get; set; }
    }
}
