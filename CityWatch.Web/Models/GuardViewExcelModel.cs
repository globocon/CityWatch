using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class GuardViewExcelModel
    {
        private readonly Guard _guard;
        private readonly IEnumerable<GuardLogin> _guardLogins;
        private readonly IEnumerable<ClientSite> _clientSites;
        private readonly IGuardDataProvider _guardDataProvider;
        public GuardViewExcelModel(Guard guard, IEnumerable<GuardLogin> guardLogins, IGuardDataProvider guardDataProvider)
        {
            _guard = guard;
            _guardLogins = guardLogins;
            _clientSites = _guardLogins.Select(z => z.ClientSite);
            _guardDataProvider = guardDataProvider;

            // Get HR statuses
            var documentStatuses = LEDStatusForLoginUser(_guard.Id);

            HR1Status = "Grey";
            HR2Status = "Grey";
            HR3Status = "Grey";

            if (documentStatuses != null && documentStatuses.Count != 0)
            {
                // Group document statuses by GroupName for faster lookups
                var statusLookup = documentStatuses.ToLookup(x => x.GroupName.Trim());

                // Set HR1Status
                var HR1List = statusLookup["HR 1 (C4i)"];
                if (HR1List.Any())
                {
                    HR1Status = HR1List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR2Status
                var HR2List = statusLookup["HR 2 (Client)"];
                if (HR2List.Any())
                {
                    HR2Status = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR3Status
                var HR3List = statusLookup["HR 3 (Special)"];
                if (HR3List.Any())
                {
                    HR3Status = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR3List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }
            }
            // Log or inspect the documentStatuses list
            //Console.WriteLine("Document Statuses:");
            //foreach (var status in documentStatuses)
            //{
            //    Console.WriteLine($"GroupName: {status.GroupName}, ColourCodeStatus: {status.ColourCodeStatus}");
            //}
            //HR1Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR1 (C4i)")?.ColourCodeStatus ?? "Grey";
            //HR2Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR2 (Client)")?.ColourCodeStatus ?? "Grey";
            //HR3Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR3 (Special)")?.ColourCodeStatus ?? "Grey";
            //Console.WriteLine($"HR1Status: {HR1Status}");
            //Console.WriteLine($"HR2Status: {HR2Status}");
            //Console.WriteLine($"HR3Status: {HR3Status}");
        }

        public int Id { get { return _guard.Id; } }

        public string Name { get { return _guard.Name; } }

        public string SecurityNo { get { return _guard.SecurityNo; } }

        public string Initial { get { return _guard.Initial; } }

        public string Pin { get { return _guard.Pin; } }

        public string State
        {
            get
            {
                var state = _guard.State;

                if (string.IsNullOrEmpty(state))
                {
                    var states = _clientSites.Select(z => z.State).Distinct().Where(z => !string.IsNullOrEmpty(z));
                    if (states.Count() == 1)
                    {
                        state = states.Single();
                    }
                }

                return state;
            }
        }

        public string Provider { get { return _guard.Provider; } }
        private string _clientSitesString;

        public string ClientSites
        {
            get
            {
                return string.IsNullOrEmpty(_clientSitesString)
                    ? string.Join(",<br />", _clientSites.Select(z => z.Name).Distinct().OrderBy(z => z))
                    : _clientSitesString;
            }
            set
            {
                _clientSitesString = value;
            }
        }
        //public string ClientSites
        //{
        //    get
        //    {
        //        return string.Join(",<br />", _clientSites.Select(z => z.Name).Distinct().OrderBy(z => z));
        //    }
        //    set { }
        //}

        public DateTime? DateEnrolled { get { return _guard.DateEnrolled; } }
        public DateTime? LoginDate
        {
            get
            {
                var latestLogin = _guardLogins.OrderByDescending(gl => gl.LoginDate).FirstOrDefault();
                return latestLogin?.LoginDate;
            }
        }
        public int? Q1JantoMarch2023
        {
            get
            {
                int totalhours = 0;
                
                // var Q1JantoMarch2023hours = _guardLogins.Where(x=> x.LoginDate.Date >= Convert.ToDateTime("01-Jan-2023").Date && x.LoginDate.Date<= Convert.ToDateTime("31-March-2023").Date).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q1JantoMarch2023hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-Jan-2023"), Convert.ToDateTime("31-March-2023")).ToList();
                var result = Q1JantoMarch2023hours
    .Select(gl => new
    {
        gl.GuardId,
        gl.ClientSiteId,
        gl.LoginDate,
        gl.OnDuty,
        gl.OffDuty,
        // Calculate DurationInSeconds
        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
        // Calculate DurationInHours
        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
        //: (gl.OffDuty - gl.OnDuty).TotalHours
        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
    })
.OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {
                    

                    totalhours = totalhours + Convert.ToInt32(item.MaxValue);
                }
                return totalhours;
            }
        }
        public int? Q2AprtoJune2023
        {
            get
            {
                int totalhours = 0;
                //var Q2AprtoJune2023hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-Apr-2023") && x.LoginDate <= Convert.ToDateTime("30-June-2023")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q2AprtoJune2023hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-Apr-2023"), Convert.ToDateTime("30-June-2023")).ToList();
                var result = Q2AprtoJune2023hours
    .Select(gl => new
    {
        gl.GuardId,
        gl.ClientSiteId,
        gl.LoginDate,
        gl.OnDuty,
        gl.OffDuty,
        // Calculate DurationInSeconds
        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
        // Calculate DurationInHours
        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
        //: (gl.OffDuty - gl.OnDuty).TotalHours
        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
    })
.OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public int? Q3JulytoSept2023
        {
            get
            {
                int totalhours = 0;
                // var Q3JulytoSept2023hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-July-2023") && x.LoginDate <= Convert.ToDateTime("30-Sep-2023")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q3JulytoSept2023hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-July-2023"), Convert.ToDateTime("30-Sep-2023")).ToList();
                var result = Q3JulytoSept2023hours
    .Select(gl => new
    {
        gl.GuardId,
        gl.ClientSiteId,
        gl.LoginDate,
        gl.OnDuty,
        gl.OffDuty,
        // Calculate DurationInSeconds
        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
        // Calculate DurationInHours
        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
        //: (gl.OffDuty - gl.OnDuty).TotalHours
        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
    })
.OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public int? Q4OcttoDec2023
        {
            get
            {
                int totalhours = 0;
                // var Q4OcttoDec2023hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-Oct-2023") && x.LoginDate <= Convert.ToDateTime("31-Dec-2023")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q4OcttoDec2023hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-Oct-2023"), Convert.ToDateTime("31-Dec-2023")).ToList();
                var result = Q4OcttoDec2023hours
    .Select(gl => new
    {
        gl.GuardId,
        gl.ClientSiteId,
        gl.LoginDate,
        gl.OnDuty,
        gl.OffDuty,
        // Calculate DurationInSeconds
        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
        // Calculate DurationInHours
        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
        //: (gl.OffDuty - gl.OnDuty).TotalHours
        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
    })
.OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public int? Q1JantoMarch2024
        {
            get
            {
                int totalhours = 0;
                //var Q1JantoMarch2024hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-Jan-2024") && x.LoginDate <= Convert.ToDateTime("31-Mar-2023")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q1JantoMarch2024hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-Jan-2024"), Convert.ToDateTime("31-Mar-2024")).ToList();
                var result = Q1JantoMarch2024hours
                    .Select(gl => new
                    {
                        gl.GuardId,
                        gl.ClientSiteId,
                        gl.LoginDate,
                        gl.OnDuty,
                        gl.OffDuty,
                        // Calculate DurationInSeconds
                        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
                        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
                        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
                        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
                        // Calculate DurationInHours
                        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
                        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
                        //: (gl.OffDuty - gl.OnDuty).TotalHours
                        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
                    })
                .OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public int? Q2AprtoJune2024
        {
            get
            {
                int totalhours = 0;
                //var Q2AprtoJune2024hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-Apr-2024") && x.LoginDate <= Convert.ToDateTime("30-June-2023")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q2AprtoJune2024hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-Apr-2024"), Convert.ToDateTime("30-June-2024")).ToList();

                var result = Q2AprtoJune2024hours
                    .Select(gl => new
                    {
                        gl.GuardId,
                        gl.ClientSiteId,
                        gl.LoginDate,
                        gl.OnDuty,
                        gl.OffDuty,
                        // Calculate DurationInSeconds
                        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
                        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
                        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
                        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
                        // Calculate DurationInHours
                        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
                        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
                        //: (gl.OffDuty - gl.OnDuty).TotalHours
                        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
                    })
                .OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public int? Q3JulytoSept2024
        {
            get
            {
                int totalhours = 0;
                // var Q3JulytoSept2024hours = _guardLogins.Where(x => x.LoginDate >= Convert.ToDateTime("01-July-2024") && x.LoginDate <= Convert.ToDateTime("30-Sep-2024")).OrderByDescending(gl => gl.LoginDate).ToList();
                var Q3JulytoSept2024hours = _guardDataProvider.GetGuardLoginsByGuardIdAndDate(_guard.Id, Convert.ToDateTime("01-July-2024"), Convert.ToDateTime("30-Sep-2024")).ToList();

                var result = Q3JulytoSept2024hours
                    .Select(gl => new
                    {
                        gl.GuardId,
                        gl.ClientSiteId,
                        gl.LoginDate,
                        gl.OnDuty,
                        gl.OffDuty,
                        // Calculate DurationInSeconds
                        DurationInSeconds = Convert.ToDateTime(gl.OffDuty).Month == gl.OnDuty.Month && Convert.ToDateTime(gl.OffDuty).Year == gl.OnDuty.Year
                        ? (int)(Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalSeconds
                        //(int)(gl.OffDuty - gl.OnDuty).TotalSeconds
                        : (int)(gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalSeconds,
                        // Calculate DurationInHours
                        DurationInHours = (Convert.ToDateTime(gl.OffDuty).Month != gl.OnDuty.Month || Convert.ToDateTime(gl.OffDuty).Year != gl.OnDuty.Year)
                        ? (gl.OnDuty.Date.AddDays(1) - gl.OnDuty).TotalHours
                        //: (gl.OffDuty - gl.OnDuty).TotalHours
                        : (Convert.ToDateTime(gl.OffDuty).Subtract(Convert.ToDateTime(gl.OnDuty))).TotalHours
                    })
                .OrderBy(gl => gl.LoginDate).ToList();
                var resultnew = result
    .GroupBy(x => x.LoginDate.Date) // Group by the date part
    .Select(g => new
    {
        Date = g.Key,                // The date
        MaxValue = Convert.ToInt32(g.Max(x => x.DurationInHours)) // Maximum value in the group
    })
    .OrderBy(x => x.Date)           // Order by date
    .ToList();
                foreach (var item in resultnew)
                {


                    totalhours = totalhours + item.MaxValue;
                }
                return totalhours;
            }
        }
        public bool IsActive { get { return _guard.IsActive; } }
        public string Email { get { return _guard.Email; } }
        public string Mobile { get { return _guard.Mobile; } }
        public bool IsRCAccess { get { return _guard.IsRCAccess; } }
        public bool IsKPIAccess { get { return _guard.IsKPIAccess; } }
        public bool IsLB_KV_IR { get { return _guard.IsLB_KV_IR; } }
        public bool IsAdminGlobal { get { return _guard.IsAdminGlobal; } }
        public bool IsAdminPowerUser { get { return _guard.IsAdminPowerUser; } }
        public bool IsSTATS { get { return _guard.IsSTATS; } }
        //p1-224 RC Bypass For HR -start
        public bool IsRCBypass { get { return _guard.IsRCBypass; } }

        public string Gender { get { return _guard.Gender; } }
        //p1-224 RC Bypass For HR -end

        public string HR1Status { get; set; }
        public string HR2Status { get; set; }
        public string HR3Status { get; set; }
        private List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            // Retrieve guard document details in one call
            var guardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            // Iterate through each document detail
            foreach (var item in guardDocumentDetails)
            {
                // Directly use the item without filtering again
                hrGroupStatusesNew.Add(new HRGroupStatusNew
                {
                    Status = 1,
                    GroupName = item.HrGroupText.Trim(), // Assuming HrGroupText replaces GroupName
                                                         // Generate the color code based on the current item
                    ColourCodeStatus = GuardledColourCodeGenerator(new List<GuardComplianceAndLicense> { item })
                });
            }

            return hrGroupStatusesNew;
        }

        private string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> selectedList)
        {
            var today = DateTime.Now;
            var colourCode = "Green"; // Default to green

            if (selectedList.Count > 0)
            {
                // Check if any entry has DateType == true
                var hasDateTypeTrue = selectedList.Any(x => x.DateType == true);

                if (hasDateTypeTrue)
                {
                    return "Green"; // Return immediately if DateType == true exists
                }

                // Get the first non-null expiry date (if any)
                var firstItem = selectedList.FirstOrDefault(x => x.ExpiryDate != null);

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here

                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        return "Red";
                    }
                    else if ((expiryDate - today).Days < 45)
                    {
                        return "Yellow";
                    }
                }
            }

            return colourCode; // Default return is green
        }

        public class HRGroupStatusNew
        {
            public int Status { get; set; }
            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }
        }
    }
}

