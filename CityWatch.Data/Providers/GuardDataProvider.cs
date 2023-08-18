using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IGuardDataProvider
    {
        List<Guard> GetGuards();
        int SaveGuard(Guard guard, out string initalsUsed);
        List<GuardLogin> GetGuardLogins(int[] guardIds);
        List<GuardLogin> GetUniqueGuardLogins();
        List<GuardLogin> GetGuardLoginsBySmartWandId(int smartWandId);
        List<GuardLogin> GetGuardLoginsByLogBookId(int logBookId);
        GuardLogin GetGuardLoginById(int id);
        GuardLogin GetGuardLastLogin(int guardId);
        int SaveGuardLogin(GuardLogin guardLogin);
        void UpdateGuardOffDuty(int guardLoginId, DateTime offDuty);
        List<GuardLicense> GetAllGuardLicenses();
        List<GuardLicense> GetGuardLicenses(int guardId);
        GuardLicense GetGuardLicense(int id);
        void SaveGuardLicense(GuardLicense guardLicense);
        void DeleteGuardLicense(int id);
        List<GuardCompliance> GetAllGuardCompliances();
        List<GuardCompliance> GetGuardCompliances(int guardId);
        GuardCompliance GetGuardCompliance(int id);
        void SaveGuardCompliance(GuardCompliance guardCompliance);
        void DeleteGuardCompliance(int id);
    }

    public class GuardDataProvider : IGuardDataProvider
    {
        private readonly CityWatchDbContext _context;

        public GuardDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<Guard> GetGuards()
        {
            return _context.Guards.ToList();
        }

        public int SaveGuard(Guard guard, out string initalsUsed)
        {
            initalsUsed = guard.Initial;
            var isNewGuard = guard.Id == -1;
            if (isNewGuard)
            {
                guard.Id = 0;
                guard.IsActive = true;
                initalsUsed = MakeGuardInitials(guard.Initial);
                guard.Initial = initalsUsed;
                guard.DateEnrolled = DateTime.Today;
                _context.Guards.Add(guard);
            }
            else
            {
                var updateGuard = _context.Guards.SingleOrDefault(x => x.Id == guard.Id);
                updateGuard.Name = guard.Name;
                updateGuard.SecurityNo = guard.SecurityNo;
                updateGuard.IsActive = guard.IsActive;
                if (updateGuard.Initial != guard.Initial)
                {
                    initalsUsed = MakeGuardInitials(guard.Initial);
                    updateGuard.Initial = initalsUsed;
                }
                else
                {
                    updateGuard.Initial = guard.Initial;
                }
                updateGuard.State = guard.State;
                updateGuard.Provider = guard.Provider;
                updateGuard.Mobile = guard.Mobile;
                updateGuard.Email = guard.Email;
                updateGuard.IsKPIAccess = guard.IsKPIAccess;
                updateGuard.IsRCAccess = guard.IsRCAccess;
            }

            _context.SaveChanges();

            if (isNewGuard)
            {
                _context.GuardLicenses.Add(new GuardLicense()
                {
                    LicenseNo = guard.SecurityNo,
                    LicenseType = GuardLicenseType.Other,
                    GuardId = guard.Id
                });
                _context.SaveChanges();
            }

            return guard.Id;
        }

        public List<GuardLogin> GetGuardLogins(int[] guardIds)
        {
            var guardLogins = _context.GuardLogins
                .Where(z => guardIds.Contains(z.GuardId))

                .Include(z => z.Guard)
                .Include(z => z.ClientSite)
                .ToList();


            return guardLogins
                .GroupBy(z => new { z.GuardId, z.ClientSiteId })
                .Select(z => z.Last())
                .ToList();
        }

        public List<GuardLogin> GetGuardLoginsBySmartWandId(int smartWandId)
        {
            return _context.GuardLogins
                .Where(z => z.SmartWandId == smartWandId)
                .ToList();
        }

        public List<GuardLogin> GetGuardLoginsByLogBookId(int logBookId)
        {
            return _context.GuardLogins
                .Where(z => z.ClientSiteLogBookId == logBookId)
                .Include(z => z.ClientSiteLogBook)
                .Include(z => z.Guard)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .ToList();
        }

        public List<GuardLogin> GetUniqueGuardLogins()
        {
            return _context.GuardLogins.ToList()
                .GroupBy(z => new { z.GuardId, z.ClientSiteId })
                .Select(z => z.First())
                .ToList();
        }

        public GuardLogin GetGuardLoginById(int id)
        {
            return _context.GuardLogins
                .Include(z => z.Guard)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .SingleOrDefault(z => z.Id == id);
        }

        public GuardLogin GetGuardLastLogin(int guardId)
        {
            return _context.GuardLogins
                .Include(z => z.ClientSite)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .Include(z => z.ClientSite.ClientType)
                .Where(z => z.GuardId == guardId)
                .OrderByDescending(z => z.LoginDate)
                .FirstOrDefault();
        }

        public int SaveGuardLogin(GuardLogin guardLogin)
        {
            if (guardLogin.Id == 0)
            {
                _context.GuardLogins.Add(guardLogin);
            }
            else
            {
                var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLogin.Id);
                if (guardLoginToUpdate != null)
                {
                    guardLoginToUpdate.ClientSiteId = guardLogin.ClientSiteId;
                    guardLoginToUpdate.LoginDate = guardLogin.LoginDate;
                    guardLoginToUpdate.SmartWandId = guardLogin.SmartWandId;
                    guardLoginToUpdate.PositionId = guardLogin.PositionId;
                    guardLoginToUpdate.OnDuty = guardLogin.OnDuty;
                }
            }
            _context.SaveChanges();
            return guardLogin.Id;
        }

        public void UpdateGuardOffDuty(int guardLoginId, DateTime offDuty)
        {
            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId);
            if (guardLoginToUpdate != null)
            {
                guardLoginToUpdate.OffDuty = offDuty;
                _context.SaveChanges();
            }
        }

        private string MakeGuardInitials(string initial)
        {
            var latestInitial = _context.Guards.Where(z => z.Initial.StartsWith(initial)).Select(y => y.Initial).OrderBy(x => x).LastOrDefault();
            if (latestInitial != null)
            {
                var nextSeqNumber = 2;
                if (latestInitial.Contains('['))
                {
                    var lastSeqNumber = int.Parse(latestInitial.Split('[', ']')[1]);
                    nextSeqNumber = lastSeqNumber + 1;
                }

                return string.Format($"{initial} [{nextSeqNumber}]");
            }

            return initial;
        }

        public GuardLicense GetGuardLicense(int id)
        {
            return _context.GuardLicenses
                .Include(z => z.Guard)
                .SingleOrDefault(z => z.Id == id);
        }

        public List<GuardLicense> GetAllGuardLicenses()
        {
            return _context.GuardLicenses.Include(z => z.Guard).ToList();
        }

        public List<GuardLicense> GetGuardLicenses(int guardId)
        {
            return _context.GuardLicenses
                .Where(x => x.GuardId == guardId)
                .Include(z => z.Guard).ToList();
        }

        public void SaveGuardLicense(GuardLicense guardLicense)
        {
            if (guardLicense.Id == 0)
            {
                _context.GuardLicenses.Add(guardLicense);
            }
            else
            {
                var guardLicenseToUpdate = _context.GuardLicenses.SingleOrDefault(x => x.Id == guardLicense.Id);
                if (guardLicenseToUpdate != null)
                {
                    guardLicenseToUpdate.LicenseNo = guardLicense.LicenseNo;
                    guardLicenseToUpdate.LicenseType = guardLicense.LicenseType;
                    guardLicenseToUpdate.Reminder1 = guardLicense.Reminder1;
                    guardLicenseToUpdate.Reminder2 = guardLicense.Reminder2;
                    guardLicenseToUpdate.ExpiryDate = guardLicense.ExpiryDate;
                    guardLicenseToUpdate.FileName = guardLicense.FileName;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteGuardLicense(int id)
        {
            var guardLicenseToDelete = _context.GuardLicenses.SingleOrDefault(x => x.Id == id);
            if (guardLicenseToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardLicenseToDelete);
            _context.SaveChanges();
        }

        public void SaveGuardCompliance(GuardCompliance guardCompliance)
        {
            if (guardCompliance.Id == 0)
            {
                _context.GuardCompliances.Add(guardCompliance);
            }
            else
            {
                var guardComplianceToUpdate = _context.GuardCompliances.SingleOrDefault(x => x.Id == guardCompliance.Id);
                if (guardComplianceToUpdate != null)
                {
                    guardComplianceToUpdate.ReferenceNo = guardCompliance.ReferenceNo;
                    guardComplianceToUpdate.Description = guardCompliance.Description;
                    guardComplianceToUpdate.Reminder1 = guardCompliance.Reminder1;
                    guardComplianceToUpdate.Reminder2 = guardCompliance.Reminder2;
                    guardComplianceToUpdate.ExpiryDate = guardCompliance.ExpiryDate;
                    guardComplianceToUpdate.FileName = guardCompliance.FileName;
                }
            }
            _context.SaveChanges();
        }

        public List<GuardCompliance> GetAllGuardCompliances()
        {
            return _context.GuardCompliances.Include(z => z.Guard).ToList();
        }

        public List<GuardCompliance> GetGuardCompliances(int guardId)
        {
            return _context.GuardCompliances
                .Include(z => z.Guard)
                .Where(x => x.GuardId == guardId)
                .OrderBy(x => x.ReferenceNo).ToList();
        }

        public GuardCompliance GetGuardCompliance(int id)
        {
            return _context.GuardCompliances
                .Include(z => z.Guard)
                .SingleOrDefault(x => x.Id == id);
        }

        public void DeleteGuardCompliance(int id)
        {
            var guardComplianceToDelete = _context.GuardCompliances.SingleOrDefault(x => x.Id == id);
            if (guardComplianceToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardComplianceToDelete);
            _context.SaveChanges();
        }
    }
}