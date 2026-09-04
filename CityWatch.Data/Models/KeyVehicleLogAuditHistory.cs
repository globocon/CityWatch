using CityWatch.Data.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class KeyVehicleLogAuditHistory
    {
        public KeyVehicleLogAuditHistory()
        { }

        public KeyVehicleLogAuditHistory(KeyVehicleLog keyVehicleLog, KeyVehicleLog keyVehicleLogFromDb)
        {
            var isCreate = keyVehicleLog.Id == 0;

            KeyVehicleLogId = keyVehicleLog.Id;
            ProfileId = 0;
            GuardLoginId = !isCreate ? keyVehicleLog.ActiveGuardLoginId.Value : keyVehicleLog.GuardLoginId;
            AuditMessage = KeyVehicleLogHelper.GetKeyVehicleLogAuditMessage(keyVehicleLog, keyVehicleLogFromDb);
            AuditTime = DateTime.Now;
            IsCreate = isCreate;
        }

        [Key]
        public int Id { get; set; }

        public int KeyVehicleLogId { get; set; }

        public int GuardLoginId { get; set; }

        public DateTime AuditTime { get; set; }

        [NotMapped]
        public string AuditTimeString { get { return AuditTime.ToString("dd MMM yyyy @ HH:mmm"); } }

        public string AuditMessage { get; set; }

        public bool IsCreate { get; set; }

        public int ProfileId { get; set; }

        [ForeignKey("GuardLoginId")]
        public GuardLogin GuardLogin { get; set; }

        [ForeignKey("KeyVehicleLogId")]
        public KeyVehicleLog KeyVehicleLog { get; set; }

        [ForeignKey("ProfileId")]
        public KeyVehicleLogProfile KeyVehicleLogProfile { get; set; }
    }
}
