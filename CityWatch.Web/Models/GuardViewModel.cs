using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class GuardViewModel
    {
        private readonly Guard _guard;
        private readonly IEnumerable<GuardLogin> _guardLogins;
        private readonly IEnumerable<ClientSite> _clientSites;
        private readonly List<LanguageDetails> _languageDetails;

        public GuardViewModel(Guard guard, IEnumerable<GuardLogin> guardLogins, List<LanguageDetails> languageDetails)
        {
            _guard = guard;
            _guardLogins = guardLogins;
            _clientSites = _guardLogins.Select(z => z.ClientSite);
            _languageDetails = languageDetails;




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

        public string hr1Description { get; set; }
        public string hr2Description { get; set; }
        public string hr3Description { get; set; }
        public bool IsSTATSChartsAccess { get { return _guard.IsSTATSChartsAccess; } }
        public bool IsRCFusionAccess { get { return _guard.IsRCFusionAccess; } }

        public bool IsAdminSOPToolsAccess { get { return _guard.IsAdminSOPToolsAccess; } }


        public bool IsAdminAuditorAccess { get { return _guard.IsAdminAuditorAccess; } }


        public bool IsAdminInvestigatorAccess { get { return _guard.IsAdminInvestigatorAccess; } }


        public bool IsAdminThirdPartyAccess { get { return _guard.IsAdminThirdPartyAccess; } }
        public bool IsRCHRAccess { get { return _guard.IsRCHRAccess; } }
        public bool IsRCLiteAccess { get { return _guard.IsRCLiteAccess; } }
        public List<LanguageDetails> languageDetails1 { get { return _languageDetails.Where(x => x.GuardId == _guard.Id).ToList(); } }
        public string languages
        {
            get
            {
                return string.Join(",", _languageDetails.Where(x => x.GuardId == _guard.Id).Select(z => z.LanguageMaster.Language).Distinct().OrderBy(z => z));
            }
        }
    }
}
