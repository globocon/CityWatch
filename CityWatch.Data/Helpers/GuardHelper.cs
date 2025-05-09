using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Helpers
{
    public class GuardHelper
    {
        public static string GetGuardDocumentDbxRootFolder(Data.Models.Guard guard)
        {
            return $"/CWS-HR/{guard.State ?? "AU"}/{guard.SecurityNo}";
        }
        public static string GetGuardDocumentDbxRootFolderNew(Data.Models.Guard guard,string DroDir)
        {
            if (!DroDir.StartsWith("/"))
            {
                DroDir = "/" + DroDir;
            }
            if (!DroDir.EndsWith("/"))
            {
                DroDir += "/";
            }
            return $"{DroDir}{guard.State ?? "AU"}/{guard.SecurityNo}";
        }

        public static string GetGuardDocumentDbxRootUrl(Data.Models.Guard guard)
        {
            return $"https://www.dropbox.com/home/CWS-HR/{guard.State ?? "AU"}/{guard.SecurityNo}";
        }
    }
}
