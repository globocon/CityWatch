using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CityWatch.Data.Models
{
    public class ClientSiteKpiSetting
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        [Display(Name = "Koios Site Id A")]
        public int? KoiosClientSiteId { get; set; }
        [Display(Name = "Koios Site Id B")]
        public int? KoiosClientSiteIdB { get; set; }

        [Display(Name = "Dropbox Directory")]
        public string DropboxImagesDir { get; set; }

        [Display(Name = "Photo Points Per Patrol")]
        public int? PhotoPointsPerPatrol { get; set; }

        [Display(Name = "WAND Points Per Patrol")]
        public int? WandPointsPerPatrol { get; set; }

        [Display(Name = "Tune Downgrade Buffer ")]
        public decimal? TuneDowngradeBuffer { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        public bool IsThermalCameraSite { get; set; }

        public string SiteImage { get; set; }

        [Display(Name = "Expected Patrol Duration (min)")]
        public int? ExpPatrolDuration { get; set; }

        [Display(Name = "Minimum Patrol Freq. (p/hr)")]
        public int? MinPatrolFreq { get; set; }

        [Display(Name = "Minimum Images per patrol")]
        public int? MinImagesPerPatrol { get; set; }         
       

        public List<ClientSiteKpiNote> Notes { get; set; }

        public List<RCActionList> RCActionList { get; set; }

        public RCActionList rclistKP
        {
            get
            {
                if (Id != 0)
                {
                    if (RCActionList != null)
                    {
                        var rcActionList = RCActionList.FirstOrDefault();
                        return rcActionList ?? new RCActionList()
                        {
                            ClientSiteID = ClientSiteId,
                            SettingsId = Id,
                            SiteAlarmKeypadCode = string.Empty,
                            Imagepath = string.Empty
                        };

                    }
                    else
                    {
                        return new RCActionList()
                        {
                            ClientSiteID = ClientSiteId,
                            SettingsId = Id,
                            SiteAlarmKeypadCode = string.Empty,
                            Imagepath = string.Empty
                        };

                    }

                }
                else
                {
                    return new RCActionList()
                    {
                        ClientSiteID = ClientSiteId,
                        SettingsId = Id,
                        SiteAlarmKeypadCode = string.Empty,
                        Imagepath = string.Empty
                    };

                }
            }
        }
        public ClientSiteKpiNote NotesForThisMonth
        {
            get
            {
                var thisMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var notesThisMonth = Notes?.SingleOrDefault(z => z.ForMonth == thisMonthDate);
                return notesThisMonth ?? new ClientSiteKpiNote()
                {
                    ForMonth = thisMonthDate,
                   Notes = string.Empty,
                    SettingsId = Id
                };
            }
        }

        public bool IsWeekendOnlySite { get; set; }

        private List<ClientSiteDayKpiSetting> _clientSiteDayKpiSettings;
        public List<ClientSiteDayKpiSetting> ClientSiteDayKpiSettings
        {
            get
            {
                if (_clientSiteDayKpiSettings == null)
                    _clientSiteDayKpiSettings = GetDefaultClientSiteDayKpiSettings();
                return _clientSiteDayKpiSettings;
            }

            set
            {
                _clientSiteDayKpiSettings = value;
            }
        }
        [NotMapped]
        public int PositionId { get; set; }
        [NotMapped]
        public string PositionGuard { get; set; }
        [NotMapped]
        public string PositionPatrolCar { get; set; }
        private List<ClientSiteManningKpiSetting> _clientSiteManningKpiSettings;
        private List<ClientSiteManningKpiSetting> _clientSiteManningPatrolCarKpiSettings;
        [NotMapped]
        public List<ClientSiteManningKpiSetting> ClientSiteManningGuardKpiSettings
        {
            get
            {
                if (_clientSiteManningKpiSettings == null || !_clientSiteManningKpiSettings.Any())
                    _clientSiteManningKpiSettings = GetDefaultClientManningDayKpiSettings();
                return _clientSiteManningKpiSettings;
            }

            set
            {
                
                    _clientSiteManningKpiSettings = value;

            }
        }
        [NotMapped]
        public List<ClientSiteManningKpiSetting> ClientSiteManningPatrolCarKpiSettings
        {
            get
            {
                if (_clientSiteManningPatrolCarKpiSettings == null || !_clientSiteManningPatrolCarKpiSettings.Any())
                    _clientSiteManningPatrolCarKpiSettings = GetDefaultClientManningDayKpiSettings();
                return _clientSiteManningPatrolCarKpiSettings;
            }

            set
            {
               
                    _clientSiteManningPatrolCarKpiSettings = value;
            }
        }

        public string ImageTargetText
        {
            get
            {

                var weekDayImageTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.WeekDay != DayOfWeek.Saturday && z.WeekDay != DayOfWeek.Sunday &&
                    z.ImagesTarget.GetValueOrDefault() > 0);
                var weekDayImageTargetUnit = weekDayImageTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                var weekEndImageTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.ImagesTarget.GetValueOrDefault() > 0 &&
                    (z.WeekDay == DayOfWeek.Saturday || z.WeekDay == DayOfWeek.Sunday));
                var weekEndImageTargetUnit = weekEndImageTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                return $"Target: M-F @ {weekDayImageTarget?.ImagesTarget} {weekDayImageTargetUnit} \n" +
                        $"Target: S-S @ {weekEndImageTarget?.ImagesTarget} {weekEndImageTargetUnit}";
            }
        }

        public string WandScanTargetText
        {
            get
            {
                var weekDayWandScanTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.WeekDay != DayOfWeek.Saturday && z.WeekDay != DayOfWeek.Sunday &&
                    z.WandScansTarget.GetValueOrDefault() > 0);
                var weekDayWandScanTargetUnit = weekDayWandScanTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                var weekEndWandScanTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.WandScansTarget.GetValueOrDefault() > 0 &&
                    (z.WeekDay == DayOfWeek.Saturday || z.WeekDay == DayOfWeek.Sunday));
                var weekEndWandScanTargetUnit = weekEndWandScanTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                return $"Target: M-F @ {weekDayWandScanTarget?.WandScansTarget} {weekDayWandScanTargetUnit} \n" +
                        $"Target: S-S @ {weekEndWandScanTarget?.WandScansTarget} {weekEndWandScanTargetUnit}";
            }
        }

        public string PatrolsTargetText
        {
            get
            {
                var weekDayPatrolsTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.WeekDay != DayOfWeek.Saturday && z.WeekDay != DayOfWeek.Sunday &&
                    z.NoOfPatrols.GetValueOrDefault() > 0);
                var weekDayPatrolsTargetUnit = weekDayPatrolsTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                var weekEndPatrolsTarget = ClientSiteDayKpiSettings
                    .FirstOrDefault(z => z.NoOfPatrols.GetValueOrDefault() > 0 &&
                    (z.WeekDay == DayOfWeek.Saturday || z.WeekDay == DayOfWeek.Sunday));
                var weekEndPatrolsTargetUnit = weekEndPatrolsTarget?.PatrolFrequency == 1 ? "p/day" : "p/hr";

                return $"Target: M-F @ {weekDayPatrolsTarget?.NoOfPatrols} {weekDayPatrolsTargetUnit} \n" +
                       $"Target: S-S @ {weekEndPatrolsTarget?.NoOfPatrols} {weekEndPatrolsTargetUnit}";
            }
        }

        private List<ClientSiteDayKpiSetting> GetDefaultClientSiteDayKpiSettings()
        {
            return new List<ClientSiteDayKpiSetting>
            {
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Monday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Tuesday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Wednesday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Thursday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Friday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Saturday},
                new ClientSiteDayKpiSetting() { WeekDay = DayOfWeek.Sunday}
            };
        }

        private List<ClientSiteManningKpiSetting> GetDefaultClientManningDayKpiSettings()
        {
            return new List<ClientSiteManningKpiSetting>
            {
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Monday,DefaultValue=true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Tuesday,DefaultValue=true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Wednesday,DefaultValue=true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Thursday, DefaultValue = true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Friday, DefaultValue = true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Saturday, DefaultValue = true},
                new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Sunday, DefaultValue = true},
                /* New Field for PHO*/
                 new ClientSiteManningKpiSetting() { WeekDay = DayOfWeek.Sunday, DefaultValue = true,IsPHO=1}


            };
        }
    }
}
