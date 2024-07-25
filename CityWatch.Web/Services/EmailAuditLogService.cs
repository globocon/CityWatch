using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using Dropbox.Api.Files;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;

public interface IEmailAuditLogService
{
    List<EmailAuditLog> GetEmailLog();
   
    void SaveEmailAuditLog(EmailAuditLog log);

    public class EmailAuditLogService : IEmailAuditLogService
    {
        private readonly IEmailLogDataProvider _emailLogDataProvider;

        public EmailAuditLogService(IEmailLogDataProvider emailLogDataProvider)
        {
            _emailLogDataProvider = emailLogDataProvider;
        }
       

        public List<EmailAuditLog> GetEmailLog()
        {
            var EmailLogs = _emailLogDataProvider.GetEmailLogs();

            return EmailLogs;
        }
        public void SaveEmailAuditLog(EmailAuditLog log)
        {
            _emailLogDataProvider.SaveEmailLog(log);
        }



    }
}
