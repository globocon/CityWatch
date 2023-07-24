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
        List<GuardLogin> GetGuardLoginsBySmartWandId(int smartWandId);
        List<GuardLogin> GetGuardLoginsByLogBookId(int logBookId);
        GuardLogin GetGuardLoginById(int id);
        GuardLogin GetGuardLastLogin(int guardId);
        int SaveGuardLogin(GuardLogin guardLogin);
        void UpdateGuardOffDuty(int guardLoginId, DateTime offDuty);
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
            if (guard.Id == -1)
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
            }

            _context.SaveChanges();
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
    }
}