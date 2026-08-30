using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Pages.Incident;
using CityWatch.Web.Services;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CityWatch.Web.Pages.Develop
{
   

   
    public class EmailCheckModel : PageModel
    {
        const string LAST_USED_IR_SEQ_NO_CONFIG_NAME = "LastUsedIrSn";

        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly EmailOptions _EmailOptions;
        private readonly IViewDataService _ViewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IIrDataProvider _irDataProvider;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IGuardLogDataProvider _guardLogDataProvider;

        public EmailCheckModel(IWebHostEnvironment webHostEnvironment,
          IOptions<EmailOptions> emailOptions,
          IViewDataService viewDataService,
          IClientDataProvider clientDataProvider,
          IIrDataProvider irDataProvider,
          IConfigDataProvider configDataProvider,
          IAppConfigurationProvider appConfigurationProvider,
          ILogger<RegisterModel> logger,
          IIncidentReportGenerator incidentReportGenerator,
          IGuardLogDataProvider guardLogDataProvider)
        {
            _WebHostEnvironment = webHostEnvironment;
            _EmailOptions = emailOptions.Value;
            _ViewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _irDataProvider = irDataProvider;
            _appConfigurationProvider = appConfigurationProvider;
            _logger = logger;
            _incidentReportGenerator = incidentReportGenerator;
            _guardLogDataProvider = guardLogDataProvider;
        }
        public string RenderedString { get; private set; }
        public void OnGet()
        {
            try
            {
                var fromAddress = _EmailOptions.FromAddress.Split('|');
                var toAddress = _EmailOptions.ToAddress.Split('|');
                var ccAddress = _EmailOptions.CcAddress.Split('|');
                var subject = _EmailOptions.Subject;
                var messageHtml = _EmailOptions.Message;
                RenderedString = _EmailOptions.FromAddress + "</br> " + _EmailOptions.ToAddress + "</br> " + _EmailOptions.CcAddress + "</br> " + _EmailOptions.Subject + "</br> " + _EmailOptions.Message;



                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                foreach (var address in GetToEmailAddressList(toAddress))
                    message.To.Add(address);





                message.Subject = $"{subject} - {"test mail"} - {"test mail"}";

                var builder = new BodyBuilder()
                {
                    HtmlBody = messageHtml
                };

                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                        !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                        client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }

            }
            catch(Exception ex)
            {
                RenderedString = RenderedString + "Error Message : " + ex.Message.ToString(); ;

                var st = new StackTrace(ex, true);
                // Get the top stack frame
                RenderedString = RenderedString+"st : " + st;
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();
                RenderedString = RenderedString +" Line :"+ line ;

            }
        }

        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            emailAddressList.Add(new MailboxAddress(toAddress[1], toAddress[0]));
            var fields = _configDataProvider.GetReportFields().ToList();
            return emailAddressList;
        }
    }
}
