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

