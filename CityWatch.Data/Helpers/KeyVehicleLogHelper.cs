using CityWatch.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Helpers
{
    public static class KeyVehicleLogHelper
    {
        public static string GetKeyVehicleLogAuditMessage(KeyVehicleLog newKeyVehicleLog, KeyVehicleLog oldKeyVehicleLog)
        {
            if (oldKeyVehicleLog == null && newKeyVehicleLog == null)
                return string.Empty;

            if (oldKeyVehicleLog == null && newKeyVehicleLog.Id == 0)
                return "Initial entry";

            var editMessages = new List<string>();
            if (!oldKeyVehicleLog.ExitTime.HasValue && newKeyVehicleLog.ExitTime.HasValue)
                editMessages.Add("Exit entry");

            if (oldKeyVehicleLog.KeyNo != newKeyVehicleLog.KeyNo)
                editMessages.Add("Key list modified");

            if (!editMessages.Any())
                editMessages.Add("Edit");

            return string.Join(", ", editMessages);
        }
    }
}
