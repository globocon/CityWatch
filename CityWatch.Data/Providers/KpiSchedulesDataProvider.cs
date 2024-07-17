using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IKpiSchedulesDataProvider
    {
        List<KpiSendSchedule> GetAllSendSchedules();
        KpiSendSchedule GetSendScheduleById(int scheduleId);
        void SaveSendSchedule(KpiSendSchedule sendSchedule, bool updateClientSites = false);
        void DeleteSendSchedule(int id);
        List<KpiSendScheduleJob> GetAllKpiSendScheduleJobs();
        int SaveSendScheduleJob(KpiSendScheduleJob sendScheduleJob);
        List<KpiSendScheduleSummaryNote> GetKpiSendScheduleSummaryNotes(int scheduleId);
        KpiSendScheduleSummaryNote GetKpiSendScheduleSummaryNote(int id);
        int SaveKpiSendScheduleSummaryNote(KpiSendScheduleSummaryNote summaryNote);
        void SaveKpiSendScheduleSummaryImage(int scheduleId, string fileName);
        KpiSendScheduleSummaryImage GetScheduleSummaryImage(int scheduleId);
        void DeleteSummaryImage(int scheduleId);
        List<KpiSendSchedule> GetAllSendSchedulesUisngGuardId(int GuardId);
        KpiSendSchedule GetSendScheduleByIdandGuardId(int scheduleId, int GuardId);
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

        public KpiSendSchedule GetSendScheduleByIdandGuardId(int scheduleId,int GuardId)
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
            foreach(var li in KpiSendSchedule.KpiSendScheduleClientSites)
            {
                if (!distinctClientSiteIds.Contains(li.ClientSiteId))
                {
                    KpiSendSchedule.KpiSendScheduleClientSites.Remove(li);

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

        public void DeleteSendSchedule(int id)
        {
            var recordToDelete = _context.KpiSendSchedules.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.KpiSendSchedules.Remove(recordToDelete);
            _context.SaveChanges();
        }

        public List<KpiSendScheduleJob> GetAllKpiSendScheduleJobs()
        {
            return _context.KpiSendScheduleJobs.ToList();
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
    }
}