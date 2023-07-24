using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;

namespace CityWatch.Web.Pages.Guard
{
    public class LoginModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IConfigDataProvider _configDataProvider;

        public LoginModel(IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IConfigDataProvider configDataProvider)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _configDataProvider = configDataProvider;
        }

        [BindProperty]
        public GuardLoginViewModel GuardLogin { get; set; }

        [BindProperty]
        public LogBookType LogBookType { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet(string t)
        {
            LogBookType = t switch
            {
                "gl" => LogBookType.DailyGuardLog,
                "vl" => LogBookType.VehicleAndKeyLog,
                _ => throw new ArgumentOutOfRangeException("Failed to identify log book type"),
            };
        }

        public JsonResult OnPostLoginGuard()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, errors = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
            }

            var smartWandOrPositionId = GetSmartWandOrPositionId();
            if (!smartWandOrPositionId.HasValue)
            {
                return new JsonResult(new { success = false, errors = new string[] { "Failed to load smart wand or position details" } });
            }

            if (GuardLogin.OffDuty <= GuardLogin.OnDuty)
            {
                GuardLogin.OffDuty = GuardLogin.OffDuty.Value.AddDays(1);
            }

            var success = false;
            var message = string.Empty;
            var guardInitals = GuardLogin.Guard.Initial;
            var initalsUsed = string.Empty;
            try
            {
                GuardLogin.SmartWandOrPositionId = smartWandOrPositionId;
                GuardLogin.ClientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Name == GuardLogin.ClientSiteName);
                if (GuardLogin.IsNewGuard)
                {
                    GuardLogin.Guard.Id = _guardDataProvider.SaveGuard(GuardLogin.Guard, out initalsUsed);
                }

                var logBookId = GetLogBookId(out var isNewLogBook);
                var guardLoginId = GetGuardLoginId(logBookId);                

                if (LogBookType == LogBookType.DailyGuardLog)
                {
                    CreateLogbookLoggedInEntry(logBookId, guardLoginId);
                }

                if (LogBookType == LogBookType.VehicleAndKeyLog && isNewLogBook)
                {
                    var previousDayLogBook = _clientDataProvider.GetClientSiteLogBook(GuardLogin.ClientSite.Id, LogBookType.VehicleAndKeyLog, DateTime.Today.AddDays(-1));
                    if (previousDayLogBook != null)
                    {
                        _viewDataService.CopyOpenLogbookEntriesFromPreviousDay(previousDayLogBook.Id, logBookId, guardLoginId);
                    }
                }

                HttpContext.Session.SetInt32("LogBookId", logBookId);
                HttpContext.Session.SetInt32("GuardLoginId", guardLoginId);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "Error: A guard with same name or security number already exists";
                }
            }

            return new JsonResult(new
            {
                success,
                message,
                LogBookType,
                initalsChangedMessage = (GuardLogin.IsNewGuard && initalsUsed != guardInitals) ? $"Guard with initials '{guardInitals}' already exists. So initals changed to '{initalsUsed}'" : string.Empty
            });
        }

        public JsonResult OnGetGuardDetails(string securityNumber)
        {
            var guard = _guardDataProvider.GetGuards().SingleOrDefault(z => string.Compare(z.SecurityNo, securityNumber, StringComparison.OrdinalIgnoreCase) == 0);
            GuardLogin lastLogin = null;
            if (guard != null)
                lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id);
            return new JsonResult(new { guard, lastLogin });
        }

        public JsonResult OnGetSmartWands(string siteName, int? guardId)
        {
            return new JsonResult(_viewDataService.GetSmartWands(siteName, guardId));
        }

        public JsonResult OnGetOfficerPositions()
        {
            var officerPositionsWithoutSelect = _viewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly).Where(z => z.Value != string.Empty);
            return new JsonResult(officerPositionsWithoutSelect);
        }

        public JsonResult OnGetCheckIsWandAvailable(string clientSiteName, string smartWandNo, int? guardId)
        {
            var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().SingleOrDefault(z => z.ClientSite.Name == clientSiteName & z.SmartWandId == smartWandNo);
            return new JsonResult(_viewDataService.CheckWandIsInUse(smartWand.Id, guardId));
        }

        private int GetGuardLoginId(int logBookId)
        {
            GuardLogin guardLogin;

            GuardLogin existingGuardLogin = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).SingleOrDefault(x => x.GuardId == GuardLogin.Guard.Id && x.OnDuty == GuardLogin.OnDuty);

            if (existingGuardLogin != null)
            {
                guardLogin = existingGuardLogin;
                guardLogin.ClientSiteId = GuardLogin.ClientSite.Id;
                guardLogin.PositionId = GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null;
                guardLogin.SmartWandId = !GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null;
                guardLogin.OnDuty = GuardLogin.OnDuty;
                guardLogin.OffDuty = GuardLogin.OffDuty;
                guardLogin.UserId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
            }
            else
            {
                guardLogin = GetNewGuardLogin();
            }

            guardLogin.ClientSiteLogBookId = logBookId;

            var guardLoginId = _guardDataProvider.SaveGuardLogin(guardLogin);

            return guardLoginId;
        }

        private int GetLogBookId(out bool isNewLogBook)
        {
            isNewLogBook = false;
            int logBookId;
            var logBook = _clientDataProvider.GetClientSiteLogBook(GuardLogin.ClientSite.Id, LogBookType, DateTime.Today);
            if (logBook == null)
            {
                logBookId = _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = GuardLogin.ClientSite.Id,
                    Type = LogBookType,
                    Date = DateTime.Today,
                });

                isNewLogBook = true;
            }
            else
            {
                logBookId = logBook.Id;
            }

            return logBookId;
        }

        private GuardLogin GetNewGuardLogin()
        {
            return new GuardLogin()
            {
                LoginDate = DateTime.Now,
                GuardId = GuardLogin.Guard.Id,
                ClientSiteId = GuardLogin.ClientSite.Id,
                PositionId = GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null,
                SmartWandId = !GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null,
                OnDuty = GuardLogin.OnDuty,
                OffDuty = GuardLogin.OffDuty,
                UserId = AuthUserHelper.LoggedInUserId.GetValueOrDefault()
            };
        }

        private int? GetSmartWandOrPositionId()
        {
            if (GuardLogin.IsPosition)
            {
                var position = _configDataProvider.GetPositions().SingleOrDefault(z => z.Name == GuardLogin.SmartWandOrPosition);
                if (position != null)
                    return position.Id;
            }
            else
            {
                var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(x => x.ClientSite.Name == GuardLogin.ClientSiteName).SingleOrDefault(z => z.SmartWandId == GuardLogin.SmartWandOrPosition);
                if (smartWand != null)
                    return smartWand.Id;
            }

            return null;
        }

        private void CreateLogbookLoggedInEntry(int logBookId, int guardLoginId)
        {
            var signInEntry = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                GuardLoginId = guardLoginId,
                EventDateTime = DateTime.Now,
                Notes = "Logbook Logged In",
                IsSystemEntry = true
            };
            _guardLogDataProvider.SaveGuardLog(signInEntry);
        }
    }
}
