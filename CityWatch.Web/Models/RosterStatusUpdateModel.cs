using CityWatch.Data.Enums;

namespace CityWatch.Web.Models
{
    public class RosterStatusUpdateModel
    {
        public int ShiftId { get; set; }
        public RosterShiftStatus NewStatus { get; set; }
        public RosterShiftStatus ExpectedStatus { get; set; }
        public int CallingGuardId { get; set; }
        public string Reason { get; set; }
    }
}
