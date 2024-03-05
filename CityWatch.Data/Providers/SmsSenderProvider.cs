using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{

    public interface ISmsSenderProvider
    {
        bool SendSms(List<SmsChannelEventLog> scev, string message, SiteEventLog svl);
    }

    public class SmsSenderProvider : ISmsSenderProvider
    {
        private readonly ISmsService _smsService;

        public SmsSenderProvider(ISmsService smsService)
        {
            _smsService = smsService;
        }

        public bool SendSms(List<SmsChannelEventLog> scev, string message, SiteEventLog svl)
        {
            var rtn = _smsService.SendSMS(scev, message, svl);
            return rtn.Result;
        }

    }

}