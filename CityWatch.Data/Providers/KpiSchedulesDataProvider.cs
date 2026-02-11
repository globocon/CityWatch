using CityWatch.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Providers
{
    public interface IKpiSchedulesDataProvider
    {
        List<KpiSendSchedule> GetAllSendSchedules();
        KpiSendSchedule GetSendScheduleById(int scheduleId);
        void SaveSendSchedule(KpiSendSchedule sendSchedule, bool updateClientSites = false);
        void DeleteSendSchedule(int id);
        void DeleteSendScheduleTimesheet(int id);
        List<KpiSendScheduleJob> GetAllKpiSendScheduleJobs();
        List<KpiSendScheduleJobsTimeSheet> GetAllKpiSendScheduleJobsTimesheet();
        int SaveSendScheduleJob(KpiSendScheduleJob sendScheduleJob);
        int SaveSendScheduleJobTimesheet(KpiSendScheduleJobsTimeSheet sendScheduleJob);
        List<KpiSendScheduleSummaryNote> GetKpiSendScheduleSummaryNotes(int scheduleId);
        KpiSendScheduleSummaryNote GetKpiSendScheduleSummaryNote(int id);
        int SaveKpiSendScheduleSummaryNote(KpiSendScheduleSummaryNote summaryNote);
        void SaveKpiSendScheduleSummaryImage(int scheduleId, string fileName);
        KpiSendScheduleSummaryImage GetScheduleSummaryImage(int scheduleId);
        void DeleteSummaryImage(int scheduleId);
        List<KpiSendSchedule> GetAllSendSchedulesUisngGuardId(int GuardId);
        List<KpiSendTimesheetSchedules> GetAllTimesheetSchedulesUisngGuardId(int GuardId);
        KpiSendSchedule GetSendScheduleByIdandGuardId(int scheduleId, int GuardId);
        void SaveTimesheetSchedule(KpiSendTimesheetSchedules sendSchedule, bool updateClientSites = false);
        List<KpiSendTimesheetSchedules> GetAllTimesheetSchedules();
        KpiSendTimesheetSchedules GetTimesheetScheduleById(int scheduleId);
        KpiSendTimesheetSchedules GetTimesheetScheduleByIdandGuardId(int scheduleId, int GuardId);
        public void RemoveAllKpiSendScheduleJobsOldNotComplete();
        void SaveKVSchedule(KpiSendKVSchedules sendSchedule, bool updateClientSites = false);
        List<KpiSendKVSchedules> GetAllKVSchedules();
        void DeleteSendScheduleKV(int id);
        KpiSendKVSchedules GetKVScheduleById(int scheduleId);
        List<KpiSendScheduleJobsKV> GetAllKpiSendScheduleJobsKV();
        int SaveSendScheduleJobKV(KpiSendScheduleJobsKV sendScheduleJob);
        List<ClientSiteKpiNote> GetClientSiteKpiNotesAndHRRecords(int id);

        public (bool success, string message, PcarRoute route) SavePcarrouteMaster(
   int? routeId, string routeName, int smartwandId);

        public bool SavePcarrouteDetails(PcarRouteDetailViewModel model);

        public List<PcarRouteGridDto> GetPCARProfilesAll();

        public bool DeletePcarrouteProfile(int routeId);
    }

    public class KpiSchedulesDataProvider : IKpiSchedulesDataProvider
    {
        private readonly CityWatchDbContext _context;

        public KpiSchedulesDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<KpiSendSchedule> GetAllSendSchedules()
        {
            return _context.KpiSendSchedules
                .Include(z => z.KpiSendScheduleSummaryImage)
                .Include(z => z.KpiSendScheduleClientSites)
                .ThenInclude(y => y.ClientSite)
                .ThenInclude(y => y.ClientType)
                .ToList();
        }
        public List<KpiSendTimesheetSchedules> GetAllTimesheetSchedules()
        {
            return _context.KpiSendTimesheetSchedules
                .Include(z => z.KpiSendTimesheetClientSites)
                .ThenInclude(y => y.ClientSite)
                .ThenInclude(y => y.ClientType)
                .ToList();
        }
        public List<KpiSendSchedule> GetAllSendSchedulesUisngGuardId(int GuardId)
        {

            var selectedSiteSchedule = new List<KpiSendSchedule>();
            var distinctClientSiteIds = _context.GuardLogins
            .Where(z => z.GuardId == GuardId)
            .Select(z => z.ClientSite.Id)
            .Distinct()
            .ToList();

            var list = _context.KpiSendSchedules
               .Include(z => z.KpiSendScheduleSummaryImage)
               .Include(z => z.KpiSendScheduleClientSites)
               .ThenInclude(y => y.ClientSite)
               .ThenInclude(y => y.ClientType)
               .ToList();

            foreach (var item in list)
            {
                foreach (var item2 in item.KpiSendScheduleClientSites)
                {

                    if (distinctClientSiteIds.Contains(item2.ClientSiteId))
                    {

                        selectedSiteSchedule.Add(item);
                    }
                    else
                    {
                        item.KpiSendScheduleClientSites.Remove(item2);
                    }
                }


            }

            return selectedSiteSchedule;
        }
        public List<KpiSendTimesheetSchedules> GetAllTimesheetSchedulesUisngGuardId(int GuardId)
        {

            var selectedSiteSchedule = new List<KpiSendTimesheetSchedules>();
            var distinctClientSiteIds = _context.GuardLogins
            .Where(z => z.GuardId == GuardId)
            .Select(z => z.ClientSite.Id)
            .Distinct()
            .ToList();

            var list = _context.KpiSendTimesheetSchedules
               .Include(z => z.KpiSendTimesheetClientSites)
               .ThenInclude(y => y.ClientSite)
               .ThenInclude(y => y.ClientType)
               .ToList();

            foreach (var item in list)
            {
                foreach (var item2 in item.KpiSendTimesheetClientSites)
                {

                    if (distinctClientSiteIds.Contains(item2.ClientSiteId))
                    {

                        selectedSiteSchedule.Add(item);
                    }
                    else
                    {
                        item.KpiSendTimesheetClientSites.Remove(item2);
                    }
                }


            }

            return selectedSiteSchedule;
        }
        public KpiSendSchedule GetSendScheduleById(int scheduleId)
        {

            return _context.KpiSendSchedules
              .Include(t => t.KpiSendScheduleSummaryImage)
              .Include(x => x.KpiSendScheduleSummaryNotes)
              .Include(z => z.KpiSendScheduleClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == scheduleId);
        }
        public KpiSendTimesheetSchedules GetTimesheetScheduleById(int scheduleId)
        {

            return _context.KpiSendTimesheetSchedules
              .Include(z => z.KpiSendTimesheetClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == scheduleId);
        }
        public KpiSendSchedule GetSendScheduleByIdandGuardId(int scheduleId, int GuardId)
        {
            var distinctClientSiteIds = _context.GuardLogins
          .Where(z => z.GuardId == GuardId)
          .Select(z => z.ClientSite.Id)
          .Distinct()
          .ToList();
            var KpiSendSchedule = _context.KpiSendSchedules
              .Include(t => t.KpiSendScheduleSummaryImage)
              .Include(x => x.KpiSendScheduleSummaryNotes)
              .Include(z => z.KpiSendScheduleClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == scheduleId);
            foreach (var li in KpiSendSchedule.KpiSendScheduleClientSites)
            {
                if (!distinctClientSiteIds.Contains(li.ClientSiteId))
                {
                    KpiSendSchedule.KpiSendScheduleClientSites.Remove(li);

                }

            }
            return KpiSendSchedule;
        }
        public KpiSendTimesheetSchedules GetTimesheetScheduleByIdandGuardId(int scheduleId, int GuardId)
        {
            var distinctClientSiteIds = _context.GuardLogins
          .Where(z => z.GuardId == GuardId)
          .Select(z => z.ClientSite.Id)
          .Distinct()
          .ToList();
            var KpiSendSchedule = _context.KpiSendTimesheetSchedules
              .Include(z => z.KpiSendTimesheetClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == scheduleId);
            foreach (var li in KpiSendSchedule.KpiSendTimesheetClientSites)
            {
                if (!distinctClientSiteIds.Contains(li.ClientSiteId))
                {
                    KpiSendSchedule.KpiSendTimesheetClientSites.Remove(li);

                }

            }
            return KpiSendSchedule;
        }
        public KpiSendScheduleSummaryImage GetScheduleSummaryImage(int scheduleId)
        {
            return _context.KpiSendScheduleSummaryImages.SingleOrDefault(x => x.ScheduleId == scheduleId);
        }

        public void DeleteSummaryImage(int scheduleId)
        {
            var imageToDelete = _context.KpiSendScheduleSummaryImages.SingleOrDefault(x => x.ScheduleId == scheduleId);
            if (imageToDelete != null)
            {
                _context.KpiSendScheduleSummaryImages.Remove(imageToDelete);
                _context.SaveChanges();
            }
        }

        public void SaveSendSchedule(KpiSendSchedule sendSchedule, bool updateClientSites = false)
        {
            var schedule = _context.KpiSendSchedules.Include(z => z.KpiSendScheduleClientSites).SingleOrDefault(z => z.Id == sendSchedule.Id);
            if (schedule == null)
                _context.Add(sendSchedule);
            else
            {
                if (updateClientSites)
                {
                    _context.KpiSendScheduleClientSites.RemoveRange(schedule.KpiSendScheduleClientSites);
                    _context.SaveChanges();
                }

                schedule.StartDate = sendSchedule.StartDate;
                schedule.EndDate = sendSchedule.EndDate;
                schedule.Frequency = sendSchedule.Frequency;
                schedule.Time = sendSchedule.Time;
                schedule.EmailTo = sendSchedule.EmailTo;
                schedule.NextRunOn = sendSchedule.NextRunOn;
                schedule.IsPaused = sendSchedule.IsPaused;
                schedule.ProjectName = sendSchedule.ProjectName;
                schedule.SummaryNote1 = sendSchedule.SummaryNote1;
                schedule.SummaryNote2 = sendSchedule.SummaryNote2;
                schedule.CoverSheetType = sendSchedule.CoverSheetType;
                schedule.EmailBcc = sendSchedule.EmailBcc;
                schedule.IsHrTimerPaused = sendSchedule.IsHrTimerPaused;
                schedule.IsCriticalDocumentDownselect = sendSchedule.IsCriticalDocumentDownselect;
                schedule.CriticalGroupNameID = sendSchedule.CriticalGroupNameID;

                if (updateClientSites)
                    schedule.KpiSendScheduleClientSites = sendSchedule.KpiSendScheduleClientSites;
            }
            _context.SaveChanges();
        }

        public void SaveTimesheetSchedule(KpiSendTimesheetSchedules sendSchedule, bool updateClientSites = false)
        {
            var schedule = _context.KpiSendTimesheetSchedules.Include(z => z.KpiSendTimesheetClientSites).SingleOrDefault(z => z.Id == sendSchedule.Id);
            if (schedule == null)
                _context.Add(sendSchedule);
            else
            {
                if (updateClientSites)
                {
                    _context.KpiSendTimesheetClientSites.RemoveRange(schedule.KpiSendTimesheetClientSites);
                    _context.SaveChanges();
                }

                schedule.StartDate = sendSchedule.StartDate;
                schedule.EndDate = sendSchedule.EndDate;
                schedule.Frequency = sendSchedule.Frequency;
                schedule.Time = sendSchedule.Time;
                schedule.EmailTo = sendSchedule.EmailTo;
                schedule.NextRunOn = sendSchedule.NextRunOn;

                schedule.ProjectName = sendSchedule.ProjectName;

                schedule.EmailBcc = sendSchedule.EmailBcc;


                if (updateClientSites)
                    schedule.KpiSendTimesheetClientSites = sendSchedule.KpiSendTimesheetClientSites;
            }
            _context.SaveChanges();
        }

        public (bool success, string message, PcarRoute route) SavePcarrouteMaster(
     int? routeId, string routeName, int smartwandId)
        {
            // Normalize name for comparison
            string normalizedName = routeName.Trim().ToLower();

            // Validation: Check duplicate name (excluding the same record when editing)
            bool nameExists = _context.PcarRoute
                .Any(r => r.Pcarroutename.ToLower() == normalizedName &&
                          (!routeId.HasValue || r.Id != routeId.Value));

            if (nameExists)
            {
                return (false, "A profile with this route name already exists.", null);
            }

            // Validation: Check SmartWand allocation (must be unique)
            //bool smartwandExists = _context.PcarRoute
            //    .Any(r => r.Smartwandallocation == smartwandId &&
            //              (!routeId.HasValue || r.Id != routeId.Value));

            //if (smartwandExists)
            //{
            //    return (false, "This SmartWand is already assigned to another route profile.", null);
            //}

            PcarRoute route;

            if (routeId.HasValue && routeId > 0)
            {
                //  EDIT EXISTING
                route = _context.PcarRoute.FirstOrDefault(r => r.Id == routeId.Value);

                if (route == null)
                    return (false, "Route not found.", null);

                route.Pcarroutename = routeName;
                route.Smartwandallocation = smartwandId;

                _context.PcarRoute.Update(route);
            }
            else
            {
                //  CREATE NEW
                route = new PcarRoute
                {
                    Pcarroutename = routeName,
                    Smartwandallocation = smartwandId
                };

                _context.PcarRoute.Add(route);
            }

            _context.SaveChanges();
            return (true, "Route saved successfully.", route);
        }


        public bool DeletePcarrouteProfile(int routeId)
        {
            try
            {
                // Load the route along with its details
                var route = _context.PcarRoute
                    .Include(r => r.RouteDetails)
                    .FirstOrDefault(r => r.Id == routeId);

                //if (route == null)
                //    return false; // No route found

                // Remove associated route details first
                if (route.RouteDetails != null && route.RouteDetails.Any())
                {
                    _context.PcarRouteDetails.RemoveRange(route.RouteDetails);
                }

                // Remove the route itself
                _context.PcarRoute.Remove(route);

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return false;
            }
        }


        public bool SavePcarrouteDetails(PcarRouteDetailViewModel model)
        {
            if (model.ClientSiteIds == null || !model.ClientSiteIds.Any())
                return false;

            try
            {
                foreach (var siteId in model.ClientSiteIds)
                {
                    // Check if a detail already exists for this route & site
                    var existingDetail = _context.PcarRouteDetails
                        .FirstOrDefault(d => d.PcarRouteId == model.PcarRouteId && d.ClientSiteId == siteId);

                    if (existingDetail != null)
                    {
                        // Update existing record
                        existingDetail.StartMon = model.StartMon;
                        existingDetail.EndMon = model.EndMon;
                        existingDetail.VisitMon = model.VisitMon;

                        existingDetail.StartTue = model.StartTue;
                        existingDetail.EndTue = model.EndTue;
                        existingDetail.VisitTue = model.VisitTue;

                        existingDetail.StartWed = model.StartWed;
                        existingDetail.EndWed = model.EndWed;
                        existingDetail.VisitWed = model.VisitWed;

                        existingDetail.StartThu = model.StartThu;
                        existingDetail.EndThu = model.EndThu;
                        existingDetail.VisitThu = model.VisitThu;

                        existingDetail.StartFri = model.StartFri;
                        existingDetail.EndFri = model.EndFri;
                        existingDetail.VisitFri = model.VisitFri;

                        existingDetail.StartSat = model.StartSat;
                        existingDetail.EndSat = model.EndSat;
                        existingDetail.VisitSat = model.VisitSat;

                        existingDetail.StartSun = model.StartSun;
                        existingDetail.EndSun = model.EndSun;
                        existingDetail.VisitSun = model.VisitSun;

                        existingDetail.StartPho = model.StartPho;
                        existingDetail.EndPho = model.EndPho;
                        existingDetail.VisitPho = model.VisitPho;
                    }
                    else
                    {
                        // Add new record
                        var detail = new PcarRouteDetails
                        {
                            PcarRouteId = model.PcarRouteId,
                            ClientSiteId = siteId,
                            StartMon = model.StartMon,
                            EndMon = model.EndMon,
                            VisitMon = model.VisitMon,

                            StartTue = model.StartTue,
                            EndTue = model.EndTue,
                            VisitTue = model.VisitTue,

                            StartWed = model.StartWed,
                            EndWed = model.EndWed,
                            VisitWed = model.VisitWed,

                            StartThu = model.StartThu,
                            EndThu = model.EndThu,
                            VisitThu = model.VisitThu,

                            StartFri = model.StartFri,
                            EndFri = model.EndFri,
                            VisitFri = model.VisitFri,

                            StartSat = model.StartSat,
                            EndSat = model.EndSat,
                            VisitSat = model.VisitSat,

                            StartSun = model.StartSun,
                            EndSun = model.EndSun,
                            VisitSun = model.VisitSun,

                            StartPho = model.StartPho,
                            EndPho = model.EndPho,
                            VisitPho = model.VisitPho
                        };

                        _context.PcarRouteDetails.Add(detail);
                    }
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Ideally log the exception
                return false;
            }
        }




        public void DeleteSendSchedule(int id)
        {
            var recordToDelete = _context.KpiSendSchedules.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.KpiSendSchedules.Remove(recordToDelete);
            _context.SaveChanges();
        }
        public void DeleteSendScheduleTimesheet(int id)
        {
            var recordToDelete = _context.KpiSendTimesheetSchedules.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.KpiSendTimesheetSchedules.Remove(recordToDelete);
            _context.SaveChanges();
        }

        public List<KpiSendScheduleJob> GetAllKpiSendScheduleJobs()
        {
            return _context.KpiSendScheduleJobs.ToList();
        }


        public void RemoveAllKpiSendScheduleJobsOldNotComplete()
        {
            // Remove all old schedules with no completion date and created before today
            var oldSchedules = _context.KpiSendScheduleJobs
                                       .Where(z => !z.CompletedDate.HasValue && z.CreatedDate.Date < DateTime.Now.Date)
                                       .ToList();

            if (oldSchedules.Any())
            {
                _context.KpiSendScheduleJobs.RemoveRange(oldSchedules);
                _context.SaveChanges();
            }
        }




        public List<KpiSendScheduleJobsTimeSheet> GetAllKpiSendScheduleJobsTimesheet()
        {
            return _context.KpiSendScheduleJobsTimeSheet.ToList();
        }
        public int SaveSendScheduleJob(KpiSendScheduleJob sendScheduleJob)
        {
            var scheduleJob = _context.KpiSendScheduleJobs.SingleOrDefault(z => z.Id == sendScheduleJob.Id);

            if (scheduleJob == null)
                _context.Add(sendScheduleJob);
            else
            {
                scheduleJob.CompletedDate = sendScheduleJob.CompletedDate;
                scheduleJob.Success = sendScheduleJob.Success;
                scheduleJob.StatusMessage = sendScheduleJob.StatusMessage;
            }
            _context.SaveChanges();

            return sendScheduleJob.Id;
        }
        public int SaveSendScheduleJobTimesheet(KpiSendScheduleJobsTimeSheet sendScheduleJob)
        {
            var scheduleJob = _context.KpiSendScheduleJobsTimeSheet.SingleOrDefault(z => z.Id == sendScheduleJob.Id);

            if (scheduleJob == null)
                _context.Add(sendScheduleJob);
            else
            {
                scheduleJob.CompletedDate = sendScheduleJob.CompletedDate;
                scheduleJob.Success = sendScheduleJob.Success;
                scheduleJob.StatusMessage = sendScheduleJob.StatusMessage;
            }
            _context.SaveChanges();

            return sendScheduleJob.Id;
        }
        public List<KpiSendScheduleSummaryNote> GetKpiSendScheduleSummaryNotes(int scheduleId)
        {
            return _context.KpiSendScheduleSummaryNotes.Where(x => x.ScheduleId == scheduleId).ToList();
        }

        public KpiSendScheduleSummaryNote GetKpiSendScheduleSummaryNote(int id)
        {
            return _context.KpiSendScheduleSummaryNotes.SingleOrDefault(x => x.Id == id);
        }

        public int SaveKpiSendScheduleSummaryNote(KpiSendScheduleSummaryNote summaryNote)
        {
            if (summaryNote.Id == 0)
                _context.KpiSendScheduleSummaryNotes.Add(summaryNote);
            else
            {
                var summaryNoteToUpdate = _context.KpiSendScheduleSummaryNotes.SingleOrDefault(x => x.Id == summaryNote.Id);
                if (summaryNoteToUpdate != null)
                    summaryNoteToUpdate.Notes = summaryNote.Notes;
            }
            _context.SaveChanges();
            return summaryNote.Id;
        }

        public void SaveKpiSendScheduleSummaryImage(int scheduleId, string fileName)
        {
            var summaryImageToUpdate = _context.KpiSendScheduleSummaryImages.SingleOrDefault(x => x.ScheduleId == scheduleId);
            if (summaryImageToUpdate != null)
            {
                summaryImageToUpdate.FileName = fileName;
                summaryImageToUpdate.LastUpdated = DateTime.Now;
            }
            else
            {
                var kpiSummaryImage = new KpiSendScheduleSummaryImage
                {
                    ScheduleId = scheduleId,
                    FileName = fileName,
                    LastUpdated = DateTime.Now
                };
                _context.KpiSendScheduleSummaryImages.Add(kpiSummaryImage);
            }
            _context.SaveChanges();
        }
        public void SaveKVSchedule(KpiSendKVSchedules sendSchedule, bool updateClientSites = false)
        {
            var schedule = _context.KpiSendKVSchedules.Include(z => z.KpiSendKVClientSites).SingleOrDefault(z => z.Id == sendSchedule.Id);
            if (schedule == null)
                _context.Add(sendSchedule);
            else
            {
                if (updateClientSites)
                {
                    _context.KpiSendKVClientSites.RemoveRange(schedule.KpiSendKVClientSites);
                    _context.SaveChanges();
                }

                schedule.StartDate = sendSchedule.StartDate;
                schedule.EndDate = sendSchedule.EndDate;
                schedule.Frequency = sendSchedule.Frequency;
                schedule.Time = sendSchedule.Time;
                schedule.EmailTo = sendSchedule.EmailTo;
                schedule.NextRunOn = sendSchedule.NextRunOn;

                schedule.ProjectName = sendSchedule.ProjectName;

                schedule.EmailBcc = sendSchedule.EmailBcc;
                schedule.CompanyName = sendSchedule.CompanyName;
                schedule.VehicleRego = sendSchedule.VehicleRego;
                schedule.KeyNo = sendSchedule.KeyNo;
                schedule.ClientSiteLocationId = sendSchedule.ClientSiteLocationId;

                if (updateClientSites)
                    schedule.KpiSendKVClientSites = sendSchedule.KpiSendKVClientSites;
            }
            _context.SaveChanges();
        }
        public List<KpiSendKVSchedules> GetAllKVSchedules()
        {
            return _context.KpiSendKVSchedules
                .Include(z => z.KpiSendKVClientSites)
                .ThenInclude(y => y.ClientSite)
                .ThenInclude(y => y.ClientType)
                .ToList();
        }


        public List<PcarRouteGridDto> GetPCARProfilesAll()
        {
            string pattern = @"\[R\d+\]";
            var routes = _context.PcarRoute
     .Include(r => r.SmartWand)  // Include linked SmartWand
     .Include(r => r.RouteDetails)
         .ThenInclude(rd => rd.ClientSite)          

     .Select(r =>  new PcarRouteGridDto
     {
         Id = r.Id,
         Pcarroutename = r.Pcarroutename,
         Smartwandallocation = r.Smartwandallocation,
         SmartWandId = r.SmartWand.SmartWandId,
         PhoneNumber = Regex.Replace(r.SmartWand.PhoneNumber, pattern, "").Trim() ,
         Sites = string.Join(", ", r.RouteDetails.Select(d => d.ClientSite.Name))
     })
     .ToList();

            return routes;
        }

        public void DeleteSendScheduleKV(int id)
        {
            var recordToDelete = _context.KpiSendKVSchedules.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.KpiSendKVSchedules.Remove(recordToDelete);
            _context.SaveChanges();
        }
        public KpiSendKVSchedules GetKVScheduleById(int scheduleId)
        {

            return _context.KpiSendKVSchedules
              .Include(z => z.KpiSendKVClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == scheduleId);
        }
        public List<KpiSendScheduleJobsKV> GetAllKpiSendScheduleJobsKV()
        {
            return _context.KpiSendScheduleJobsKV.ToList();
        }
        public int SaveSendScheduleJobKV(KpiSendScheduleJobsKV sendScheduleJob)
        {
            var scheduleJob = _context.KpiSendScheduleJobsKV.SingleOrDefault(z => z.Id == sendScheduleJob.Id);

            if (scheduleJob == null)
                _context.Add(sendScheduleJob);
            else
            {
                scheduleJob.CompletedDate = sendScheduleJob.CompletedDate;
                scheduleJob.Success = sendScheduleJob.Success;
                scheduleJob.StatusMessage = sendScheduleJob.StatusMessage;
            }
            _context.SaveChanges();

            return sendScheduleJob.Id;
        }
        public List<ClientSiteKpiNote> GetClientSiteKpiNotesAndHRRecords(int id)
        {
            return _context.ClientSiteKpiNotes.Where(z => z.Id == id).ToList();
        }

    }


    public class PcarRouteGridDto
    {
        public int Id { get; set; }
        public string Pcarroutename { get; set; }
        public int Smartwandallocation { get; set; }
        public string SmartWandId { get; set; }
        public string PhoneNumber { get; set; }
        public string Sites { get; set; }
    }



}