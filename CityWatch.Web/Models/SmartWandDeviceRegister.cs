
namespace CityWatch.Web.Models
{
    public class SmartWandDeviceRegister
    {
        public int SmartWandId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string? Message { get; set; }
    }
}
