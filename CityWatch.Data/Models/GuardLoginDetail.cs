using System;

namespace CityWatch.Data.Models
{
    public class GuardLoginDetail
    {
        public GuardLoginDetail(GuardLogin guardLogin)
        {
            var logBookDate = guardLogin.ClientSiteLogBook.Date.Date;
            var onDutyValue = guardLogin.OnDuty;
            var offDutyValue = guardLogin.OffDuty.Value;
           
            if (onDutyValue.Date < logBookDate)
            {
                onDutyValue = onDutyValue.Date.AddDays(1) + new TimeSpan(00, 01, 0);
            }
            if (offDutyValue.Date > logBookDate)
            {
                offDutyValue = offDutyValue.Date.AddDays(-1) + new TimeSpan(23, 59, 0);
            }

            IsPosition = guardLogin.PositionId.HasValue;
            SmartWandOrPosition = guardLogin.PositionId.HasValue ? guardLogin.Position.Name : guardLogin.SmartWand.SmartWandId;
            OnDuty = onDutyValue;
            OffDuty = offDutyValue;
            GuardName = guardLogin.Guard.Name;
        }

        public bool IsPosition { get; set; }

        public string SmartWandOrPosition { get; set; }

        public DateTime OnDuty { get; set; }

        public DateTime? OffDuty { get; set; }

        public string GuardName { get; set; }
    }
}
