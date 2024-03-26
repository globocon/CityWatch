using SMSGlobal.api;
using System.Threading.Tasks;

namespace CityWatch.Common.Services
{

    public interface ISmsGlobalService
    {
        Task<SMSGlobal.Response.SMS> SendSMSApi(string smsnumber, string smsmsg, string sendingFrom, string SmsGlobalApiKey, string SmsGlobalApiSecret);
        Task<SMSGlobal.Response.SMSId> GetMsgStatus(string smsid, string SmsGlobalApiKey, string SmsGlobalApiSecret);
        Task<SMSGlobal.Response.SMS> GetAllMsgStatus(string SmsGlobalApiKey, string SmsGlobalApiSecret);
    }

    public class SmsGlobalService: ISmsGlobalService
    {  
        public async Task<SMSGlobal.Response.SMS> SendSMSApi(string smsnumber, string smsmsg, string sendingFrom,
                                                             string SmsGlobalApiKey,string SmsGlobalApiSecret)
        {
            var client = new Client(new Credentials(SmsGlobalApiKey, SmsGlobalApiSecret));
            var response = await client.SMS.SMSSend(new
            {
                origin = sendingFrom, // Max 11 character
                destination = smsnumber,
                message = smsmsg
            });

            return response;
        }

        public async Task<SMSGlobal.Response.SMSId> GetMsgStatus(string smsid, string SmsGlobalApiKey, string SmsGlobalApiSecret)
        {
            var client = new Client(new Credentials(SmsGlobalApiKey, SmsGlobalApiSecret));
            var response = await client.SMS.SMSGetId(smsid);
            return response;
        }


        public async Task<SMSGlobal.Response.SMS> GetAllMsgStatus(string SmsGlobalApiKey, string SmsGlobalApiSecret)
        {
            string filter = "limit=20";
            var client = new Client(new Credentials(SmsGlobalApiKey, SmsGlobalApiSecret));
            var response = await client.SMS.SMSGetAll(filter);
            return response;
        }

    }
}
