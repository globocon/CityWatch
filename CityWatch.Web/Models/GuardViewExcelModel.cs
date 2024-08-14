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
            // Log or inspect the documentStatuses list
            Console.WriteLine("Document Statuses:");
            foreach (var status in documentStatuses)
            {
                Console.WriteLine($"GroupName: {status.GroupName}, ColourCodeStatus: {status.ColourCodeStatus}");
            }
            HR1Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR1 (C4i)")?.ColourCodeStatus ?? "Grey";
            HR2Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR2 (Client)")?.ColourCodeStatus ?? "Grey";
            HR3Status = documentStatuses.FirstOrDefault(ds => ds.GroupName == "HR3 (Special)")?.ColourCodeStatus ?? "Grey";
            Console.WriteLine($"HR1Status: {HR1Status}");
            Console.WriteLine($"HR2Status: {HR2Status}");
            Console.WriteLine($"HR3Status: {HR3Status}");
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

        public string ClientSites
        {
            get
            {
                return string.Join(",<br />", _clientSites.Select(z => z.Name).Distinct().OrderBy(z => z));
            }
        }

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
            var MasterGroup = _guardDataProvider.GetHRDescFull();
            var GuardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            foreach (var item in MasterGroup)
            {
                var TemDescription = item.ReferenceNo + " " + item.Description.Trim();
                var SelectedGuardDocument = GuardDocumentDetails.Where(x => x.Description == TemDescription).ToList();

                if (SelectedGuardDocument.Count > 0)
                {
                    hrGroupStatusesNew.Add(new HRGroupStatusNew
                    {
                        Status = 1,
                        GroupName = item.GroupName.Trim(),
                        ColourCodeStatus = GuardledColourCodeGenerator(SelectedGuardDocument)
                    });
                }
            }
            return hrGroupStatusesNew;
        }

        private string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> SelectedList)
        {
            var today = DateTime.Now;
            var ColourCode = "Green";

            if (SelectedList.Count > 0)
            {
                var SelectDatatype = SelectedList.Where(x => x.DateType == true).ToList();
                if (SelectDatatype.Count > 0)
                {
                    ColourCode = "Green";
                }
                else
                {
                    if (SelectedList.FirstOrDefault() != null)
                    {
                        if (SelectedList.FirstOrDefault().ExpiryDate != null)
                        {
                            var ExpiryDate = SelectedList.FirstOrDefault().ExpiryDate;
                            var timeDifference = ExpiryDate - today;

                            if (ExpiryDate < today)
                            {
                                ColourCode = "Red";
                            }
                            else if ((ExpiryDate - DateTime.Now).Value.Days < 45)
                            {
                                ColourCode = "Yellow";
                            }
                        }
                    }
                }
            }
            return ColourCode;
        }

        public class HRGroupStatusNew
        {
            public int Status { get; set; }
            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }
        }
    }
}

