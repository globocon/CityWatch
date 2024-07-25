using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using Dropbox.Api.Files;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CityWatch.Data.Providers
{
    public interface IEmailLogDataProvider
    {
        List<EmailAuditLog> GetEmailLogs();
        //List<EmailAuditLog> GetEmailLogsSuccess();
        //List<EmailAuditLog> GetEmailLogsFail();
        void SaveEmailLog(EmailAuditLog emailLog);
    }
    public class EmailLogDataProvider : IEmailLogDataProvider
    {
        private readonly CityWatchDbContext _context;

        public EmailLogDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<EmailAuditLog> GetEmailLogs()
        {
            return _context.EmailAuditLog.ToList();
        }

        public void SaveEmailLog(EmailAuditLog emailLog)
        { 
        
                if (emailLog == null)
                    throw new ArgumentNullException();
           
                _context.EmailAuditLog.Add(emailLog);           

                _context.SaveChanges();
                  
        }
    }

}

