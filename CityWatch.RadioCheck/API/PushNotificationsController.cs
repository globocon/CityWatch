using CityWatch.RadioCheck.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
namespace CityWatch.RadioCheck.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class PushNotificationsController : ControllerBase
    {
        private readonly IPushNotificationServicecs _pushNotificationService;

        public PushNotificationsController(IPushNotificationServicecs pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;

        }

        [Route("[action]", Name = "SendActionListLater")]
        [HttpGet]
        public bool SendActionListLater()
        {

            try
            {
                _pushNotificationService.SendActionListLater();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
