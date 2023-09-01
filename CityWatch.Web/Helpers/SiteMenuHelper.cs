using System;
using System.Text;
using CityWatch.Web.Helpers;

namespace CityWatch.Web.Helpers
{
    public static class SiteMenuHelper
    {
        private static readonly bool IsDevEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() != "production";

        public static string GetMenuHtml(string pageName)
        {
            if (string.IsNullOrEmpty(pageName) || pageName == PageNameHelper.Login)
                return string.Empty;

            var menuHtml = new StringBuilder();

            if (pageName != PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/"" class=""nav-link py-0""><i class=""fa fa-home mr-2""></i>Home Page</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/ControlRoom"" class=""nav-link py-0""><img src = ""/images/control-center-i.png"" alt=""control room icon"" class=""mr-2""/>Control Room</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""https://app.guardhousehq.com/au/account/login/"" class=""nav-link py-0"" target=""_blank""><i class=""fa fa-building mr-2""></i>Rosters</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/Guard/Login?t=gl"" class=""nav-link py-0""><i class=""fa fa-list-alt mr-2""></i>Daily Log Book (LB)</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index || pageName == PageNameHelper.Notify || pageName == PageNameHelper.Settings)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/Incident/Register"" class=""nav-link py-0""><i class=""fa fa-file-pdf-o mr-2""></i>Incident Report (IR)</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/Guard/Login?t=vl"" class=""nav-link py-0""><i class=""fa fa-key mr-2""></i>Key & Vehicle Register (KV)</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Index)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""Incident/DownloadSelecter"" class=""nav-link py-0""><i class=""fa fa-download mr-2""></i>Downloads</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.ControlRoom)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""https://citywatch.koios.pl"" class=""nav-link py-0"" target=""_blank""><img src=""/images/wand-scanner-i.png"" alt=""smart wands icon"" class=""mr-2""/>Smart WANDs (SW)</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.ControlRoom)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""https://wds-pr-wocausnz.anz.msiwoc.com/WebDispatcher/v5/index.html"" class=""nav-link py-0"" target=""_blank""><i class=""fa fa-map-marker mr-2"" aria-hidden=""true""></i>Radio PTToC (GPS)</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.ControlRoom)
            {
                menuHtml.AppendLine("<div>");
                if (!AuthUserHelper.IsAdminUserLoggedIn)
                {   /* Check the guard Login Conformation*/
                    menuHtml.AppendLine(@"<a href=""/Radio/Check"" id=""LoginConformationBtnRC"" class=""nav-link py-0""><i class=""fa fa-user mr-2""></i>Radio Checklist (RC)</a>");
                }
                else
                {
                    menuHtml.AppendLine(@"<a href=""/Radio/Check"" class=""nav-link py-0"" ><i class=""fa fa-user mr-2""></i>Radio Checklist (RC)</a>");
                }
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.ControlRoom)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/Reports/PatrolData"" class=""nav-link py-0""><i class=""fa fa-car mr-2""></i>Patrols & Alarm Statistics</a>");
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.ControlRoom)
            {
                var kpiWebUrl = IsDevEnv ? string.Empty : "//kpi.cws-ir.com";
                menuHtml.AppendLine("<div>");
                if (!AuthUserHelper.IsAdminUserLoggedIn)
                {   /* Check the guard Login Conformation*/
                    menuHtml.AppendFormat(@"<a href=""{0}"" class=""nav-link py-0"" id=""LoginConformationBtnKPI"" target=""_blank""><i class=""fa fa-bar-chart mr-2""></i>Telematics (KPI)</a>", string.Empty).AppendLine();

                }
                else
                {
                    menuHtml.AppendFormat(@"<a href=""{0}"" class=""nav-link py-0"" target=""_blank""><i class=""fa fa-bar-chart mr-2""></i>Telematics (KPI)</a>", kpiWebUrl).AppendLine();

                }
                menuHtml.AppendLine("</div>");
            }

            if (pageName == PageNameHelper.Register || pageName == PageNameHelper.Notify)
            {
                menuHtml.AppendLine("<div>");
                menuHtml.AppendLine(@"<a href=""/Admin/Settings"" class=""nav-link py-0""><i class=""fa fa-cog mr-2""></i>Report Settings</a>");
                menuHtml.AppendLine("</div>");
            }

            if (AuthUserHelper.IsAdminUserLoggedIn)
            {
                if (pageName == PageNameHelper.DailyGuardLog || pageName == PageNameHelper.AuditSiteLogs)
                {
                    menuHtml.AppendLine("<div>");
                    menuHtml.AppendLine(@"<a href=""/Admin/GuardSettings"" class=""nav-link py-0""><i class=""fa fa-cog mr-2""></i>Guard & Site Settings</a>");
                    menuHtml.AppendLine("</div>");
                }

                if (pageName == PageNameHelper.DailyGuardLog || pageName == PageNameHelper.GuardSettings)
                {
                    menuHtml.AppendLine("<div>");
                    menuHtml.AppendLine(@"<a href=""/Admin/AuditSiteLog"" class=""nav-link py-0""><i class=""fa fa-list-alt mr-2""></i>Audit Site Logs</a>");
                    menuHtml.AppendLine("</div>");
                }
            }

            return menuHtml.ToString();
        }
    }

    public static class PageNameHelper
    {
        public const string Index = "Index";
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string UnAuthorized = "UnAuthorized";
        public const string Settings = "Settings";
        public const string GuardSettings = "GuardSettings";
        public const string Register = "Register";
        public const string Notify = "Notify";
        public const string Downloads = "Downloads";
        public const string PatrolData = "PatrolData";
        public const string ControlRoom = "ControlRoom";
        public const string AuditSiteLogs = "AuditSiteLogs";
        public const string DailyGuardLog = "DailyGuardLog";
        public const string VehicleAndKeyLog = "VehicleAndKeyLog";
        public const string RadioCheck = "RadioCheck";
    }
}
