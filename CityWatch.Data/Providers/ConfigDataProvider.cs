using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using Dropbox.Api.Users;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.BC;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dropbox.Api.Team.GroupSelector;
using static Dropbox.Api.TeamLog.EventCategory;

namespace CityWatch.Data.Providers
{
    public interface IConfigDataProvider
    {
        List<FeedbackTemplate> GetFeedbackTemplates();
        List<FeedbackType> GetFeedbackTypes();
        int GetFeedbackTypesId(string Name);
        void SaveFeedbackTemplate(FeedbackTemplate template);
        void DeleteFeedbackTemplate(int id);
        ReportTemplate GetReportTemplate();
        void SaveReportTemplate(DateTime dateTimeUpdated);
        List<StaffDocument> GetStaffDocuments();
        void SaveStaffDocument(StaffDocument staffdocument);
        void DeleteStaffDocument(int id);
        List<State> GetStates();
        List<IncidentReportField> GetReportFields();
        List<IncidentReportField> GetReportFieldsByType(ReportFieldType type);
        void SaveReportField(IncidentReportField incidentReportField);
        void DeleteReportField(int id);
        List<IncidentReportPosition> GetPositions();
        List<ClientSiteSmartWand> GetSmartWandsDetails(int ClientSiteID);
        List<IncidentReportPSPF> GetPSPF();
        int GetLastValue();
        int OnGetMaxIdIR();
        void SavePostion(IncidentReportPosition incidentReportPosition);
        void SavePSPF(IncidentReportPSPF incidentReportPSPF);
        void DeletePSPF(int id);
        string GetPSPFName(string name);
        string GetPSPFNameFromId(int pspfid);
        void UpdateDefault();
        void DeletePosition(int id);
        void CrPrimaryLogoUpload(DateTime dateTimeUploaded, string primaryLogoPath);
        List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int LogId);
        List<StaffDocument> GetStaffDocumentsUsingType(int type);
        //to get functions for settings in radio check-start
        List<RadioCheckStatusColor> GetRadioCheckStatusColorCode(string name);
        List<RadioCheckStatus> GetRadioCheckStatusWithOutcome();
        int GetRadioCheckStatusCount();
        List<SelectListItem> GetRadioCheckStatusForDropDown(bool withoutSelect = false, bool includeVisualPrefix = false);

        //to get functions for settings in radio check-end
        //broadcast banner live events-start
        List<BroadcastBannerLiveEvents> GetBroadcastLiveEvents();
        List<BroadcastBannerLiveEvents> GetBroadcastLiveEventsByDate();
        //broadcast banner live events-end
        //broadcast banner calendar events-start
        int GetCalendarEventsCount();
        List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEvents();
        List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEventsByDate();
        string GetBroadcastLiveEventsNotExpired();
        string GetUrlsInsideBroadcastLiveEventsNotExpired();
        string GetBroadcastLiveEventsWeblink();

        void SaveDefaultEmail(string DefaultEmail);
        //broadcast banner calendar events-end
        //broadcast banner calendar events-end
        //SW Channels-start
        public List<SWChannels> GetSWChannels();
        //SW Channels-end
        //General Feeds-start
        List<GeneralFeeds> GetGeneralFeeds();
        //General Feeds-end
        public List<SmsChannel> GetSmsChannels();
        public IncidentReportPosition GetIsLogbookData(string Name);
        List<HrSettings> GetHRSettings();
        List<LicenseTypes> GetLicensesTypes();
        KeyVehcileLogField GetKVLogField();
        List<KeyVehicleLogVisitorPersonalDetail> GetProviderList(int ID);

        //p1-191 hr files task 3-end
        List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "");
        List<SelectListItem> GetDescList(int HRGroupId);
        void SaveCriticalDoc(CriticalDocuments CriticalDoc, bool updateClientSites = false);
        List<CriticalDocuments> GetCriticalDocs();
        public CriticalDocuments GetCriticalDocById(int CriticalID);
        public CriticalDocuments GetCriticalDocByIdandGuardId(int CriticalID, int GuardId);
        void DeleteCriticalDoc(int id);
        public HrSettings GetHrSettingById(int CriticalID);
        void RemoveCriticalDownSelect(CriticalDocuments CriticalDoc);

        public void UpdateStaffDocumentModuleType(StaffDocument staffdocument);

        public List<StaffDocument> GetStaffDocumentModuleDetails();

        public List<StaffDocument> GetStaffDocumentSOPDocDetails(int clientSiteId);

        public int SaveSubDomain(SubDomain subDomain);

        public SubDomain GetSubDomainDetails(string domain);

        List<HrSettings> GetHRSettingsWithHRLockEnable();

        List<HrSettings> GetHRSettingsUsingGroupId(int hrgroupId, string searchKeyNo);

        string GetClientTypeNameById(int id);

        List<string> GetCompanyDetailsUsingFilter(int[] clientSiteIds, string searchKeyNo);
        List<TrainingCourses> GetCourseDocsUsingSettingsId(int type);
        List<TrainingTQNumbers> GetTQNumbers();

        List<CriticalDocuments> GetCriticalDocsByClientSiteId(int clientSiteId);

        List<int> GetCriticalDocDescriptionIdsForClientSiteIds(int[] clientSiteIds);

        public void SaveTrainingCourses(TrainingCourses trainingCourses);
        int GetLastTQNumber(int hrsettingsid);
        List<TrainingTestQuestionSettings> GetTQSettings(int hrSettingsId);
        int GetNextQuestionWithinSameTQNumber(int hrsettingsid, int tqNumberId);
        int GetQuestionsCount(int hrsettingsid, int tqNumberId);
        int GetLastTQNumberFromQuestions(int hrsettingsid);
        TrainingTestQuestions GetTrainingQuestions(int hrsettingsid, int tqNumberId, int questionumberId);
        List<TrainingTestQuestionsAnswers> GetTrainingQuestionsAnswers(int id);
        //int GetLastFeedbackQNumbers(int hrsettingsid);
        int GetLastFeedbackQNumbers();
        //TrainingTestFeedbackQuestions GetFeedbackQuestions(int hrsettingsid, int questionumberId);
        TrainingTestFeedbackQuestions GetFeedbackQuestions(int questionumberId);
        //int GetFeedbackQuestionsCount(int hrsettingsid);
        int GetFeedbackQuestionsCount();
        List<TrainingTestFeedbackQuestionsAnswers> GetTrainingFeedbackQuestionsAnswers(int id);
        List<TrainingCourses> GetCourseDocuments();
        void DeleteCourseDocument(int id);

        public List<KPITelematicsField> GetTelematicsList();
        public KPITelematicsField GetTelematicsMobileNo(int Id);

        public List<StaffDocument> GetStaffDocumentsUsingType(int type, string query);

        /* p7-141 Multimedia & Training categories-start */
        public List<StaffDocument> GetStaffDocumentsUsingTypeAndCategory(int type, string query, int? categoryId);
        public List<StaffDocumentCategory> GetStaffDocumentCategories(int documentType);
        /* p7-141 Multimedia & Training categories-end */

        /* p1-sub-category-maintenance-start */
        public void SaveStaffDocumentCategory(StaffDocumentCategory staffDocumentCategory);
        public void DeleteStaffDocumentCategory(int id);
        public int GetStaffDocumentCountForCategory(int categoryId);
        /* p1-sub-category-maintenance-end */

        public List<StaffDocument> GetStaffDocumentsUsingTypeNew(int type, int ClientSiteID);

        public ClientSite GetClientSiteLandline(int ClientSiteID);
        public List<ClientSiteSmartWand> GetClientSiteSmartwands(int ClientSiteID);

        List<TrainingCourseInstructor> GetCourseInstructor(int type);
        void SaveTrainingCourseInstructor(TrainingCourseInstructor trainingCourseInstructor);
        List<TrainingCourseCertificate> GetCourseCertificateDocsUsingSettingsId(int type);
        void SaveTrainingCourseCertificate(TrainingCourseCertificate trainingCourseCertificate);
        List<TrainingCourseCertificate> GetCourseCertificateDocuments();
        void DeleteCourseCertificateDocument(int id);
        StaffDocument GetStaffDocumentsID(int ClientSiteID);

        //p5-Issue-2-start
        List<HrSettings> GetTrainingCoursesStatusWithOutcome(int hrgroupid);

        //p7-CourseLibraryPopupFix-start
        // Course Library popup only - same rule as the Admin "HR Groups - Course Library Only" screen
        List<HrSettings> GetCourseLibraryHrSettings(int hrgroupid);
        //p7-CourseLibraryPopupFix-end

        List<SelectListItem> GetHRGroupsDropDown(bool withoutSelect = true);
        void SaveGuardTrainingAndAssessmentTab(GuardTrainingAndAssessment trainingAssessment);
        //p5-Issue-2-end
        List<TrainingCourseCertificateRPL> GetCourseCertificateRPLUsingId(int id);

        TrainingTestQuestions GetGuardQuestions(int hrSettingsId, int tqNumberId, int guardId);
        List<TrainingTestQuestionsAnswers> GetGuardOptions(int questionId);
        void SaveGuardAnswers(GuardTrainingAttendedQuestionsAndAnswers attendedQuestions);
        int GetQuestionCount(int hrSettingsId, int tqNumberId);
        List<GuardTrainingAttendedQuestionsAndAnswers> GetQuestionNumber(int hrSettingsId, int tqNumberId, int guardId);
        List<TrainingCourses> GetTrainingCourses(int hrSettingsId, int tqNumberId);
        List<GuardTrainingAttendedQuestionsAndAnswers> GetGuardCorrectQuestions(int guardId, int trainingCourseId);
        void SaveGuardTestScores(GuardTrainingAndAssessmentScore scoreObtained);
        List<GuardTrainingAndAssessmentScore> GetGuardScores(int guardId, int trainingCourseId);
        void DeleteGuardAttendedQuestions(int guardId, int trainingCourseId);
        void DeleteGuardScores(int guardId, int trainingCourseId);
        GuardTrainingAndAssessment ReturnCourseTestStatusTostart(int guardId, int trainingCourseId);

        void SaveGuardTrainingStartTest(GuardTrainingStartTest guardTrainingStartTest);
        List<GuardTrainingStartTest> GetGuardTrainingStartTest(int guardId, int trainingCourseId);
        List<TrainingCourses> GetTrainingCoursesWithHrSettingsId(int hrSettingsId);
        List<GuardTrainingAttendedQuestionsAndAnswers> GetGuardAttendedQuestionsAndanswers(int guardId, int trainingCourseId);
        TrainingTestFeedbackQuestions GetGuardFeedbackQuestions(int hrSettingsId, int guardId);
        List<TrainingTestFeedbackQuestionsAnswers> GetGuardFeedbackOptions(int questionId);
        //int GetFeedbackQuestionCount(int hrSettingsId);
        int GetFeedbackQuestionCount();
        List<GuardTrainingAttendedFeedbackQuestionsAndAnswers> GetFeedbackQuestionNumber(int hrSettingsId, int guardId);
        void SaveGuardFeedbackAnswers(GuardTrainingAttendedFeedbackQuestionsAndAnswers attendedQuestions);

        public SubDomain GetSubDomainID(int? TypeID);
        public List<DuressAppField> GetDuressAppFields();
        List<DuressAppField> GetDuressAppByType(int type, int? profileId = null);
        public bool AddDuressSetting(DuressSetting setting);
        DuressSetting GetDuressSetting(int clientSiteId, int siteDuressNumber);
        public DuressSetting GetDuressSettingById(int duressAppId);
        public bool UpdateDuressSetting(DuressSetting setting);
        public bool DeleteDuressSettingById(int duressAppId);
        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId);
        public ClientSiteMobileAppSettings SaveCrowdSettingForSite(ClientSiteMobileAppSettings csmacs);
        public ClientSiteMobileAppSettings UpdateCrowdSettingForSite(ClientSiteMobileAppSettings csmacs);
        List<TrainingCourses> GetTrainingCoursesWithCourseId(int courseId);

        void SaveGuardTrainingPracticalDetails(GuardTrainingAndAssessmentPractical trainingAssessment);
        List<GuardTrainingAndAssessmentPractical> GetGuardTrainingPracticalDetails(int guardId, int hrsettingsId);
        List<TrainingCourses> GetTrainingCoursesWithOnlyHrSettingsId(int hrSettingsId);

        List<GuardTrainingAndAssessmentScore> GetallGuardAttendedCourse(int guardId);

        List<TrainingTestQuestions> GetTrainingQuestionsWithHRSettings(int hrsettingsid);
        public HandoverNotes GetHandoverNotes(int CientSiteID);
        List<TrainingTestQuestions> GetTrainingQuestionsWithHRAndTQSettings(int hrsettingsid, int tqNumberId);




        public void SaveDefaultEmailThirdPartyDomains(string defaultEmail, int domainId, string fileName);
        List<TrainingTestQuestions> GetTrainingTestQuestionsColor(int hrsettingsId);
        List<GuardTrainingAttendedFeedbackQuestionsAndAnswers> GetGuardAttendedFeedBackQuestionsAndanswers(int guardId, int hrsettingsId);
        public Task<List<ClientSiteMobileAppSettings>> GetAllCrowdControlSite();
        public Task<List<ClientSiteMobileAppTimeZoneDTO>> GetClientSitesTimeZones();
        public Task<List<ClientSiteMobileCrowdControl>> GetAllCurrentCrowdControlData();
        public Task SaveCrowdControlHistory(ClientSiteMobileCrowdControlHistory history);
        public Task ResetSiteAndGuardCrowdControlData(ClientSiteMobileCrowdControl csmcc, DateTime ArchivedOn, string ArchivedMode);
        public void DeleteTQSettings(int id);
        List<TrainingTestQuestions> GetTrainingTestQuestionsWithHrSettings(int hrsettingsId);
        void DeleteGuardAttendedFeedbackQuestions(int guardId, int hrsettingsId);
        public Task<List<ClientSiteMobileCrowdControlHistory>> GetCrowdControlHistoryDataForSite(int siteId);
        public Task<List<ClientSiteMobileCrowdControlGuards>> GetCrowdControlLocationDataForSite(int siteId);
        public Task<ClientSiteMobileCrowdControl> GetCrowdControlDataForSite(int siteId);
        List<TrainingTestQuestions> GetTrainingTestQuestions();
        List<TrainingCourseInstructor> GetCourseAllInstructor();
        List<SmartWandTagsType> GetSmartWandTagsType();
        List<RCActionListMessages> GetRCActionListMessages(int[] Ids);
        List<RCActionListMessagesClientsites> GetRCActionListMessagesClientSites();

        public List<ClientSiteSmartWand> GetSmartWandList();

        public JsonResult GetPCARRouteDetails(int id);

        public bool RemovePCarDeatils(int PcarRouteId, List<int> siteIds);

        List<PayRate> GetPayRates();
        void SavePayRate(PayRate payRate);
        void DeletePayRate(int id);
        List<PayRateGroup> GetPayRateGroups();
        void SavePayRateGroup(PayRateGroup group);
        void DeletePayRateGroup(int id);
        void SavePayRateGroupSites(int groupId, List<int> siteIds);
        List<Allowance> GetAllowances();
        void SaveAllowance(Allowance allowance);
        void DeleteAllowance(int id);
        List<object> GetCriticalDocsUsingClientSiteIds(int[] clientSiteIds);
        void SaveOnboardUsersTrainingAndAssessmentTab(OnBoardUsersTrainingAndAssessment trainingAssessment);
        List<OnBoardUsersTrainingAndAssessment> GetOnBoardUsersTrainingAndAssessment(int userId);
    }

    public class ConfigDataProvider : IConfigDataProvider
    {
        private readonly CityWatchDbContext _context;

        public ConfigDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<PayRate> GetPayRates()
        {
            return _context.PayRates.Include(x => x.PayRateGroup).Where(x => !x.IsDeleted).OrderBy(x => x.Description).ToList();
        }

        public void SavePayRate(PayRate payRate)
        {
            if (_context.PayRates.Any(x => x.Description == payRate.Description && x.Id != payRate.Id && !x.IsDeleted))
            {
                throw new Exception("A Pay Rate with this description already exists.");
            }

            if (payRate.Id == 0)
            {
                _context.PayRates.Add(payRate);
            }
            else
            {
                var existing = _context.PayRates.SingleOrDefault(x => x.Id == payRate.Id);
                if (existing != null)
                {
                    existing.Description = payRate.Description;
                    existing.PayRateGroupId = payRate.PayRateGroupId;
                    existing.SellRateToClient = payRate.SellRateToClient;
                    existing.Comms1 = payRate.Comms1;
                    existing.Comms2 = payRate.Comms2;
                    existing.GuardPayRate = payRate.GuardPayRate;
                    existing.Currency = payRate.Currency;
                }
            }
            _context.SaveChanges();
        }

        public void DeletePayRate(int id)
        {
            var existing = _context.PayRates.SingleOrDefault(x => x.Id == id);
            if (existing != null)
            {
                existing.IsDeleted = true;
                _context.SaveChanges();
            }
        }

        public List<PayRateGroup> GetPayRateGroups()
        {
            return _context.PayRateGroups
                .Include(x => x.PayRateGroupSites)
                    .ThenInclude(x => x.ClientSite)
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public void SavePayRateGroup(PayRateGroup group)
        {
            if (_context.PayRateGroups.Any(x => x.Name == group.Name && x.Id != group.Id && !x.IsDeleted))
            {
                throw new Exception("A Group with this name already exists.");
            }

            if (group.Id == 0)
            {
                _context.PayRateGroups.Add(group);
            }
            else
            {
                var existing = _context.PayRateGroups.SingleOrDefault(x => x.Id == group.Id);
                if (existing != null)
                {
                    existing.Name = group.Name;
                }
            }
            _context.SaveChanges();
        }

        public void DeletePayRateGroup(int id)
        {
            var existing = _context.PayRateGroups.SingleOrDefault(x => x.Id == id);
            if (existing != null)
            {
                // Optionally check if any pay rates are using this group
                if (_context.PayRates.Any(x => x.PayRateGroupId == id && !x.IsDeleted))
                {
                    throw new Exception("Cannot delete group because it is being used by one or more pay rates.");
                }
                existing.IsDeleted = true;
                _context.SaveChanges();
            }
        }

        public void SavePayRateGroupSites(int groupId, List<int> siteIds)
        {
            // Use AsNoTracking for the existence check to ensure we're checking the actual DB state
            if (groupId <= 0 || !_context.PayRateGroups.AsNoTracking().Any(x => x.Id == groupId))
            {
                throw new Exception("Invalid Pay Rate Group ID or Group does not exist. Group ID: " + groupId);
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Remove all existing assignments for this group
                        var existingAssignments = _context.PayRateGroupSites.Where(x => x.PayRateGroupId == groupId).ToList();
                        _context.PayRateGroupSites.RemoveRange(existingAssignments);
                        _context.SaveChanges(); // Commit removals first

                        // 2. Add new assignments
                        if (siteIds != null && siteIds.Any())
                        {
                            var newAssignments = siteIds.Distinct().Select(siteId => new PayRateGroupSite
                            {
                                PayRateGroupId = groupId,
                                ClientSiteId = siteId
                            }).ToList();

                            _context.PayRateGroupSites.AddRange(newAssignments);
                            _context.SaveChanges();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        var siteList = siteIds != null ? string.Join(",", siteIds) : "null";
                        throw new Exception($"Error saving site assignments for Group ID: {groupId}. Sites: [{siteList}]. Original Error: {ex.Message}", ex);
                    }
                }
            });
        }

        public List<Allowance> GetAllowances()
        {
            return _context.Allowances.Where(x => !x.IsDeleted).OrderBy(x => x.Description).ToList();
        }

        public void SaveAllowance(Allowance allowance)
        {
            if (_context.Allowances.Any(x => x.Description == allowance.Description && x.Id != allowance.Id && !x.IsDeleted))
            {
                throw new Exception("An Allowance with this profile name already exists.");
            }

            if (allowance.Id == 0)
            {
                _context.Allowances.Add(allowance);
            }
            else
            {
                var existing = _context.Allowances.SingleOrDefault(x => x.Id == allowance.Id);
                if (existing != null)
                {
                    existing.Description = allowance.Description;
                    existing.FQ = allowance.FQ;
                    existing.SellRateToClient = allowance.SellRateToClient;
                    existing.Comms1 = allowance.Comms1;
                    existing.Comms2 = allowance.Comms2;
                    existing.GuardPayRate = allowance.GuardPayRate;
                    existing.Currency = allowance.Currency;
                    existing.UpdatedDate = DateTime.Now;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteAllowance(int id)
        {
            var existing = _context.Allowances.SingleOrDefault(x => x.Id == id);
            if (existing != null)
            {
                existing.IsDeleted = true;
                existing.UpdatedDate = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public List<FeedbackTemplate> GetFeedbackTemplates()
        {
            return _context.FeedbackTemplates.Where(x => x.DeleteStatus == 0).OrderBy(x => x.Name).ToList();
        }
        //to retrieve the feedback type-start
        public List<FeedbackType> GetFeedbackTypes()
        {

            return _context.FeedbackType.OrderBy(x => x.Name).ToList();
        }
        //to retrieve the feedback type-end
        //to retrieve the feedback type id -start
        public int GetFeedbackTypesId(string Name)
        {
            return _context.FeedbackType.Where(x => x.Name == Name).Select(x => x.Id).FirstOrDefault();
        }
        //to retrieve the feedback type id -end

        public void SaveFeedbackTemplate(FeedbackTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException();

            if (template.Id == 0)
            {
                _context.FeedbackTemplates.Add(new FeedbackTemplate()
                {
                    Name = template.Name,
                    Text = template.Text,
                    Type = template.Type,
                    BackgroundColour = template.BackgroundColour,
                    TextColor = template.TextColor,
                    DeleteStatus = 0,
                    SendtoRC = template.SendtoRC
                });
            }
            else
            {
                var templateToUpdate = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == template.Id && x.DeleteStatus == 0);
                if (templateToUpdate == null)
                    throw new InvalidOperationException();

                templateToUpdate.Text = template.Text;
                templateToUpdate.Type = template.Type;
                templateToUpdate.BackgroundColour = template.BackgroundColour;
                templateToUpdate.TextColor = template.TextColor;
                templateToUpdate.DeleteStatus = 0;
                templateToUpdate.SendtoRC = template.SendtoRC;
            }
            _context.SaveChanges();
        }

        public void DeleteFeedbackTemplate(int id)
        {
            if (id == -1)
                return;

            var templateToDelete = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == id && x.DeleteStatus == 0);
            if (templateToDelete == null)
                throw new InvalidOperationException();
            //Not Delete data just change Status 0 to 1 
            templateToDelete.DeleteStatus = 1;
            _context.SaveChanges();
            //_context.FeedbackTemplates.Remove(templateToDelete);
            //_context.SaveChanges();
        }

        public ReportTemplate GetReportTemplate()
        {
            return _context.ReportTemplates.Where(x => x.SubDomainId == 0).FirstOrDefault();
        }

        public void SaveReportTemplate(DateTime dateTimeUpdated)
        {
            // Use FirstOrDefault instead of Single to prevent "Sequence contains more than one element"
            // if multiple template records exist.
            var templateToUpdate = _context.ReportTemplates.FirstOrDefault();
            if (templateToUpdate != null)
            {
                templateToUpdate.LastUpdated = dateTimeUpdated;
                _context.SaveChanges();
            }
        }
        //To save the DefaultEmail
        public void SaveDefaultEmail(string DefaultEmail)
        {
            // Use FirstOrDefault instead of Single to prevent crashing if multiple records exist.
            var templateToUpdate = _context.ReportTemplates.FirstOrDefault();
            if (templateToUpdate != null)
            {
                templateToUpdate.DefaultEmail = DefaultEmail;
                _context.SaveChanges();
            }
        }



        public void SaveDefaultEmailThirdPartyDomains(string defaultEmail, int domainId, string fileName)
        {
            var dateTimeUpdated = DateTime.Now;
            var defaultPath = "/httpdocs/wwwroot/Pdf/Template"; // Hardcoded default path

            var existingTemplate = _context.ReportTemplates
                .FirstOrDefault(x => x.SubDomainId == domainId);

            if (existingTemplate != null)
            {
                if (!string.IsNullOrWhiteSpace(defaultEmail))
                    existingTemplate.DefaultEmail = defaultEmail;

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    existingTemplate.FileName = fileName;
                    existingTemplate.LastUpdated = dateTimeUpdated; // Update timestamp only if fileName is provided
                }

                existingTemplate.Path = defaultPath; // Set the path explicitly
            }
            else
            {


                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    _context.ReportTemplates.Add(new ReportTemplate
                    {
                        SubDomainId = domainId,
                        DefaultEmail = !string.IsNullOrWhiteSpace(defaultEmail) ? defaultEmail : null,
                        FileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : null,
                        Path = defaultPath, // Set the path explicitly
                        LastUpdated = dateTimeUpdated  // Only set LastUpdated if fileName exists
                    });

                }
                else
                {
                    _context.ReportTemplates.Add(new ReportTemplate
                    {
                        SubDomainId = domainId,
                        DefaultEmail = !string.IsNullOrWhiteSpace(defaultEmail) ? defaultEmail : null,
                        FileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : null,
                        Path = defaultPath, // Set the path explicitly
                        LastUpdated = dateTimeUpdated  // Only set LastUpdated if fileName exists
                    });


                }
            }

            _context.SaveChanges();
        }




        public List<State> GetStates()
        {
            /*
            Note: 
            1. If an new state is added then this needs to be manually added in table HrSettingsClientStates for all hrid which  has IsAllStateEnabled
               in the table HrSettings             
            2. Check if these needs to be added in GuardLogDataProvider.GetStates() method
            */
            return new List<State>()
            {
                new State() { Name = "ACT" },
                new State() { Name = "NSW" },
                new State() { Name = "NT" },
                new State() { Name = "QLD" },
                new State() { Name = "SA" },
                new State() { Name = "TAS" },
                new State() { Name = "VIC" },
                new State() { Name = "WA" }
            }
            .OrderBy(x => x.Name)
            .ToList();
        }

        public List<StaffDocument> GetStaffDocuments()
        {
            return _context.StaffDocuments.OrderBy(x => x.FileName).ToList();
        }

        public List<StaffDocument> GetStaffDocumentsUsingType(int type)
        {
            // Retrieve documents of the specified type
            var staffDocList = _context.StaffDocuments
                .Where(x => x.DocumentType == type)
                .ToList();
            // Check if any of the documents have a ClientSite assigned.
            bool hasClientSite = staffDocList.Any(doc => doc.ClientSite.HasValue);

            // If there are documents with ClientSite, retrieve their names and client types.
            if (hasClientSite)
            {
                foreach (var doc in staffDocList)
                {
                    if (doc.ClientSite.HasValue)
                    {
                        // Fetch the ClientSite along with ClientType using Include
                        var clientSite = _context.ClientSites
                            .Where(x => x.Id == doc.ClientSite)
                            .Include(x => x.ClientType)
                            .FirstOrDefault();

                        // Set the properties if ClientSite is found
                        if (clientSite != null)
                        {
                            doc.ClientSiteName = clientSite.Name ?? "Unknown";
                            doc.ClientTypeName = clientSite.ClientType?.Name ?? "Unknown";
                        }
                    }
                }

                // Sort the staff document list by ClientSite and then by ClientTypeName
                staffDocList = staffDocList
                    .OrderBy(x => x.ClientTypeName)
                    .ThenBy(x => x.ClientSiteName)
                    .ToList();
            }
            else
            {
                // If no ClientSite, just order by FileName
                staffDocList = staffDocList.OrderBy(x => x.FileName).ToList();
            }

            return staffDocList;
        }



        public List<StaffDocument> GetStaffDocumentsUsingType(int type, string query)
        {
            // Retrieve documents of the specified type
            var staffDocList = _context.StaffDocuments
                .Where(x => x.DocumentType == type)
                .ToList();

            // For type 4, filter by ClientSiteName or FileName
            if (type == 4 && !string.IsNullOrEmpty(query))
            {
                staffDocList = staffDocList
                    .Where(x => x.FileName.Contains(query) ||
                                (x.ClientSite.HasValue && _context.ClientSites
                                    .Where(cs => cs.Id == x.ClientSite)
                                    .Any(cs => cs.Name.Contains(query))))
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(query))
            {
                // For other types, filter by FileName only
                staffDocList = staffDocList
                    .Where(x => x.FileName.Contains(query))
                    .ToList();
            }

            // Check if any of the documents have a ClientSite assigned
            bool hasClientSite = staffDocList.Any(doc => doc.ClientSite.HasValue);

            if (hasClientSite)
            {
                foreach (var doc in staffDocList)
                {
                    if (doc.ClientSite.HasValue)
                    {
                        // Fetch the ClientSite along with ClientType using Include
                        var clientSite = _context.ClientSites
                            .Where(x => x.Id == doc.ClientSite)
                            .Include(x => x.ClientType)
                            .FirstOrDefault();

                        // Set the properties if ClientSite is found
                        if (clientSite != null)
                        {
                            doc.ClientSiteName = clientSite.Name ?? "Unknown";
                            doc.ClientTypeName = clientSite.ClientType?.Name ?? "Unknown";
                        }
                    }
                }

                // Sort the staff document list by ClientTypeName and ClientSiteName
                staffDocList = staffDocList
                    .OrderBy(x => x.ClientTypeName)
                    .ThenBy(x => x.ClientSiteName)
                    .ToList();
            }
            else
            {
                // If no ClientSite, just order by FileName
                staffDocList = staffDocList.OrderBy(x => x.FileName).ToList();
            }

            return staffDocList;
        }

        /* p7-141 Multimedia & Training categories-start */
        public List<StaffDocument> GetStaffDocumentsUsingTypeAndCategory(int type, string query, int? categoryId)
        {
            /* Re-uses the existing type/query filtering and then narrows down to a single category.
               Documents saved before categories existed (CategoryId is null) are shown under the
               default (first) category of the type so nothing disappears from the field. */
            var staffDocList = GetStaffDocumentsUsingType(type, query);

            if (categoryId.HasValue && categoryId.Value != 0)
            {
                var defaultCategoryId = GetDefaultStaffDocumentCategoryId(type);
                staffDocList = staffDocList
                    .Where(x => x.CategoryId == categoryId.Value ||
                                (!x.CategoryId.HasValue && categoryId.Value == defaultCategoryId))
                    .ToList();
            }

            var categories = GetStaffDocumentCategories(type);
            foreach (var doc in staffDocList)
            {
                doc.CategoryName = categories.SingleOrDefault(x => x.Id == doc.CategoryId)?.Name;
            }

            return staffDocList;
        }

        public List<StaffDocumentCategory> GetStaffDocumentCategories(int documentType)
        {
            return _context.StaffDocumentCategories
                .Where(x => x.DocumentType == documentType && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();
        }

        /* The first (lowest sort order) category of a type is the default one */
        private int GetDefaultStaffDocumentCategoryId(int documentType)
        {
            return GetStaffDocumentCategories(documentType).Select(x => x.Id).FirstOrDefault();
        }
        /* p7-141 Multimedia & Training categories-end */

        /* p1-sub-category-maintenance-start */
        /* Add or rename a sub category. Files point at the Id, so a rename never moves a file. */
        public void SaveStaffDocumentCategory(StaffDocumentCategory staffDocumentCategory)
        {
            var name = staffDocumentCategory.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Please enter a name.");
            if (name.Length > 100)
                throw new ArgumentException("Name cannot be longer than 100 characters.");

            var isDuplicate = _context.StaffDocumentCategories
                .Any(x => x.DocumentType == staffDocumentCategory.DocumentType &&
                          x.IsActive &&
                          x.Id != staffDocumentCategory.Id &&
                          x.Name.Trim().ToLower() == name.ToLower());
            if (isDuplicate)
                throw new ArgumentException("That name is already used. Please choose a different one.");

            if (staffDocumentCategory.Id <= 0)
            {
                var lastSortOrder = _context.StaffDocumentCategories
                    .Where(x => x.DocumentType == staffDocumentCategory.DocumentType)
                    .Select(x => (int?)x.SortOrder)
                    .Max() ?? 0;

                _context.StaffDocumentCategories.Add(new StaffDocumentCategory()
                {
                    Name = name,
                    DocumentType = staffDocumentCategory.DocumentType,
                    SortOrder = lastSortOrder + 1,
                    IsActive = true
                });
            }
            else
            {
                var categoryToUpdate = _context.StaffDocumentCategories
                    .SingleOrDefault(x => x.Id == staffDocumentCategory.Id);
                if (categoryToUpdate == null)
                    throw new ArgumentException("That sub category no longer exists.");
                categoryToUpdate.Name = name;
            }

            _context.SaveChanges();
        }

        /* Soft remove - the row stays so any file still pointing at it can be recovered. */
        public void DeleteStaffDocumentCategory(int id)
        {
            var categoryToDelete = _context.StaffDocumentCategories.SingleOrDefault(x => x.Id == id);
            if (categoryToDelete == null)
                throw new ArgumentException("That sub category no longer exists.");

            if (GetStaffDocumentCountForCategory(id) > 0)
                throw new ArgumentException("This sub category still has files in it. Move or delete the files first.");

            /* Uploads need somewhere to land, so never remove the last one of a module. */
            var remaining = GetStaffDocumentCategories(categoryToDelete.DocumentType).Count(x => x.Id != id);
            if (remaining == 0)
                throw new ArgumentException("At least one sub category must be kept.");

            categoryToDelete.IsActive = false;
            _context.SaveChanges();
        }

        /* Counts the files the user can actually see under this sub category. Documents saved
           before sub categories existed have a null CategoryId and are displayed under the
           default one, so they have to be counted against it. */
        public int GetStaffDocumentCountForCategory(int categoryId)
        {
            var category = _context.StaffDocumentCategories.SingleOrDefault(x => x.Id == categoryId);
            if (category == null)
                return 0;

            var count = _context.StaffDocuments.Count(x => x.CategoryId == categoryId);

            if (categoryId == GetDefaultStaffDocumentCategoryId(category.DocumentType))
            {
                count += _context.StaffDocuments
                    .Count(x => x.DocumentType == category.DocumentType && x.CategoryId == null);
            }

            return count;
        }
        /* p1-sub-category-maintenance-end */

        public List<StaffDocument> GetStaffDocumentsUsingTypeNew(int type, int ClientSiteID)
        {
            var staffDocList = _context.StaffDocuments
               .Where(x => x.DocumentType == type && x.ClientSite == ClientSiteID)
               .ToList();
            return staffDocList;
        }

        public void SaveStaffDocument(StaffDocument staffdocument)
        {
            if (staffdocument.Id == 0)
            {
                _context.StaffDocuments.Add(staffdocument);
            }
            else
            {
                var documentToUpdate = _context.StaffDocuments.SingleOrDefault(x => x.Id == staffdocument.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.FileName = staffdocument.FileName;
                    documentToUpdate.LastUpdated = staffdocument.LastUpdated;
                    documentToUpdate.SOP = staffdocument.SOP;
                    documentToUpdate.ClientSite = staffdocument.ClientSite;
                    documentToUpdate.FilePath = staffdocument.FilePath;
                    // documentToUpdate.SubDomainId = staffdocument.SubDomainId;
                }
            }
            _context.SaveChanges();
        }


        public int SaveSubDomain(SubDomain subDomain)
        {
            var status = 0;
            var subDomainToUpdate = _context.SubDomain.SingleOrDefault(x => x.Id == subDomain.Id);

            if (subDomainToUpdate != null)
            {
                // Check if another subdomain with the same domain exists
                var ifAlreadyExist = _context.SubDomain.Any(x => x.Domain == subDomain.Domain && x.Id != subDomain.Id);

                if (!ifAlreadyExist)
                {
                    // Update only if no conflicting domain exists
                    subDomainToUpdate.Domain = subDomain.Domain;
                    subDomainToUpdate.Logo = subDomain.Logo;
                    subDomainToUpdate.Enabled = subDomain.Enabled;
                    subDomainToUpdate.TypeId = subDomain.TypeId;
                    _context.SubDomain.Update(subDomainToUpdate);
                    status = 1;
                }
            }
            else
            {
                // Check if the domain already exists before adding a new subdomain
                var ifAlreadyExist = _context.SubDomain.Any(x => x.Domain == subDomain.Domain);

                if (!ifAlreadyExist)
                {
                    _context.SubDomain.Add(subDomain);
                    status = 1;
                }
            }

            _context.SaveChanges();
            return status;
        }

        public void UpdateStaffDocumentModuleType(StaffDocument staffdocument)
        {
            if (staffdocument.Id != 0)
            {
                var documentToUpdate = _context.StaffDocuments.SingleOrDefault(x => x.Id == staffdocument.Id);
                if (documentToUpdate != null)
                {
                    /*Only update module typle field*/
                    documentToUpdate.LastUpdated = staffdocument.LastUpdated;
                    documentToUpdate.DocumentModuleName = staffdocument.DocumentModuleName;
                }
            }

            _context.SaveChanges();
        }

        public void DeleteStaffDocument(int id)
        {
            var docToDelete = _context.StaffDocuments.SingleOrDefault(x => x.Id == id);
            if (docToDelete == null)
                throw new InvalidOperationException();

            _context.StaffDocuments.Remove(docToDelete);
            _context.SaveChanges();
        }

        public List<IncidentReportField> GetReportFields()
        {
            return _context.IncidentReportFields.OrderBy(x => x.TypeId).ThenBy(x => x.Name).ToList();
        }
        public List<DuressAppField> GetDuressAppFields()
        {
            return _context.DuressAppField.OrderBy(x => x.TypeId).ThenBy(x => x.Name).ToList();
        }
        public List<KeyVehicleLogVisitorPersonalDetail> GetProviderList(int ID)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails.Where(x => x.PersonType == ID).OrderBy(x => x.CompanyName).ToList();
        }
        public KeyVehcileLogField GetKVLogField()
        {
            return _context.KeyVehcileLogFields.Where(x => x.Name == "CRM (Supplier/Partner)").FirstOrDefault();
        }

        public List<IncidentReportField> GetReportFieldsByType(ReportFieldType type)
        {
            return GetReportFields().Where(x => x.TypeId == type).OrderBy(x => x.Name).ToList();
        }

        public void DeleteReportField(int id)
        {
            if (id == -1)
                return;

            var reportFieldToDelete = _context.IncidentReportFields.SingleOrDefault(x => x.Id == id);
            if (reportFieldToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportFields.Remove(reportFieldToDelete);
            _context.SaveChanges();
        }

        public void SaveReportField(IncidentReportField incidentReportField)
        {
            if (incidentReportField == null)
                throw new ArgumentNullException();
            if (incidentReportField.Id == -1)
            {
                _context.IncidentReportFields.Add(new IncidentReportField()
                {
                    Name = incidentReportField.Name,
                    TypeId = incidentReportField.TypeId,
                    EmailTo = incidentReportField.EmailTo,
                    ClientSiteIds = incidentReportField.ClientSiteIds,
                    ClientTypeIds = incidentReportField.ClientTypeIds,
                    StampRcLogbook = incidentReportField.StampRcLogbook
                });
            }
            else
            {
                var reportFieldToUpdate = _context.IncidentReportFields.SingleOrDefault(x => x.Id == incidentReportField.Id);
                if (reportFieldToUpdate == null)
                    throw new InvalidOperationException();
                reportFieldToUpdate.Name = incidentReportField.Name;
                reportFieldToUpdate.TypeId = incidentReportField.TypeId;
                reportFieldToUpdate.EmailTo = incidentReportField.EmailTo;
                reportFieldToUpdate.ClientSiteIds = incidentReportField.ClientSiteIds;
                reportFieldToUpdate.ClientTypeIds = incidentReportField.ClientTypeIds;
                reportFieldToUpdate.StampRcLogbook = incidentReportField.StampRcLogbook;

            }
            _context.SaveChanges();
        }
        //code added For PSPF Report start
        public void UpdateDefault()
        {
            var pspfDefaultval = _context.IncidentReportPSPF.Where(z => z.IsDefault == true).Select(z => z.Id).FirstOrDefault();
            if (pspfDefaultval != 0)
            {
                var PSPFToUpdate = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == pspfDefaultval);
                PSPFToUpdate.IsDefault = false;
            }
            _context.SaveChanges();
        }
        public string GetPSPFName(string name)
        {
            return _context.IncidentReportPSPF.Where(x => x.Name == name).Select(x => x.Name).FirstOrDefault();

        }
        public string GetPSPFNameFromId(int id)
        {
            return _context.IncidentReportPSPF.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault();

        }
        public List<IncidentReportPSPF> GetPSPF()
        {
            return _context.IncidentReportPSPF.OrderBy(z => z.ReferenceNo).ToList();
        }
        public int GetLastValue()
        {
            return _context.IncidentReportPSPF.Count();
        }

        public int OnGetMaxIdIR()
        {
            var incidentReportid = _context.IncidentReportFields.Max(x => (int?)x.Id);
            return Convert.ToInt32(incidentReportid);

        }
        public void SavePSPF(IncidentReportPSPF incidentReportPSPF)
        {
            if (incidentReportPSPF.Id == -1)
            {
                _context.IncidentReportPSPF.Add(new IncidentReportPSPF()
                {
                    ReferenceNo = incidentReportPSPF.ReferenceNo,
                    Name = incidentReportPSPF.Name,
                    IsDefault = incidentReportPSPF.IsDefault
                });
            }
            else
            {
                var PSPFToUpdate = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == incidentReportPSPF.Id);
                if (PSPFToUpdate != null)
                {
                    PSPFToUpdate.ReferenceNo = incidentReportPSPF.ReferenceNo;
                    PSPFToUpdate.Name = incidentReportPSPF.Name;
                    PSPFToUpdate.IsDefault = incidentReportPSPF.IsDefault;
                }
            }
            _context.SaveChanges();
        }
        public void DeletePSPF(int id)
        {
            if (id == -1)
                return;

            var PSPFToDelete = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == id);
            if (PSPFToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportPSPF.Remove(PSPFToDelete);
            _context.SaveChanges();
        }
        //code added For PSPF Report stop
        public List<IncidentReportPosition> GetPositions()
        {
            return _context.IncidentReportPositions.OrderBy(z => z.Name).ToList();
        }
        public List<ClientSiteSmartWand> GetSmartWandsDetails(int ClientSiteID)
        {
            return _context.ClientSiteSmartWands.Where(z => z.ClientSiteId == ClientSiteID && z.IsDeleted == false).ToList();
        }

        //To get the Logbbok Data-p6-101 start
        public IncidentReportPosition GetIsLogbookData(string Name)
        {
            return _context.IncidentReportPositions.Where(x => x.Name == Name).FirstOrDefault();
        }
        //To get the Logbbok Data-p6-101 stop
        public void SavePostion(IncidentReportPosition incidentReportPosition)
        {
            var ClientSiteName = _context.ClientSites.Where(x => x.Id == incidentReportPosition.ClientsiteId).FirstOrDefault();

            if (incidentReportPosition.Id == -1)
            {
                if (ClientSiteName != null)
                {
                    _context.IncidentReportPositions.Add(new IncidentReportPosition()
                    {
                        Name = incidentReportPosition.Name,
                        EmailTo = incidentReportPosition.EmailTo,
                        IsPatrolCar = incidentReportPosition.IsPatrolCar,
                        DropboxDir = incidentReportPosition.DropboxDir,
                        IsLogbook = incidentReportPosition.IsLogbook,
                        ClientsiteId = incidentReportPosition.ClientsiteId,
                        ClientsiteName = ClientSiteName.Name,
                        IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass
                    });
                }
                else
                {
                    _context.IncidentReportPositions.Add(new IncidentReportPosition()
                    {
                        Name = incidentReportPosition.Name,
                        EmailTo = incidentReportPosition.EmailTo,
                        IsPatrolCar = incidentReportPosition.IsPatrolCar,
                        DropboxDir = incidentReportPosition.DropboxDir,
                        IsLogbook = incidentReportPosition.IsLogbook,
                        ClientsiteId = incidentReportPosition.ClientsiteId,
                        ClientsiteName = null,
                        IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass
                    });

                }
            }
            else
            {
                var positionToUpdate = _context.IncidentReportPositions.SingleOrDefault(x => x.Id == incidentReportPosition.Id);
                if (positionToUpdate != null)
                {
                    positionToUpdate.Name = incidentReportPosition.Name;
                    positionToUpdate.EmailTo = incidentReportPosition.EmailTo;
                    positionToUpdate.IsPatrolCar = incidentReportPosition.IsPatrolCar;
                    positionToUpdate.DropboxDir = incidentReportPosition.DropboxDir;
                    positionToUpdate.IsLogbook = incidentReportPosition.IsLogbook;
                    positionToUpdate.ClientsiteId = incidentReportPosition.ClientsiteId;
                    if (ClientSiteName != null)
                    {
                        positionToUpdate.ClientsiteName = ClientSiteName.Name;
                    }
                    else
                    {
                        positionToUpdate.ClientsiteName = null;
                    }
                    positionToUpdate.IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass;

                }
            }
            _context.SaveChanges();
        }

        public void DeletePosition(int id)
        {
            if (id == -1)
                return;

            var positionToDelete = _context.IncidentReportPositions.SingleOrDefault(x => x.Id == id);
            if (positionToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportPositions.Remove(positionToDelete);
            _context.SaveChanges();
        }

        public void CrPrimaryLogoUpload(DateTime dateTimeUploaded, string primaryLogoPath)
        {
            // Use FirstOrDefault instead of Single to prevent "Sequence contains more than one element"
            // if multiple company details records exist.
            var templateToUpdate = _context.CompanyDetails.FirstOrDefault();
            if (templateToUpdate != null)
            {
                templateToUpdate.PrimaryLogoUploadedOn = dateTimeUploaded;
                templateToUpdate.PrimaryLogoPath = primaryLogoPath;
                _context.SaveChanges();
            }
        }
        public List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int LogId)
        {
            return _context.IncidentReportsPlatesLoaded.Where(z => z.LogId == LogId).OrderBy(z => z.Id).ToList();
        }
        //to get functions for settings in radio check-start
        public List<RadioCheckStatusColor> GetRadioCheckStatusColorCode(string name)
        {
            //To get the Name in order
            var filteredRecords = _context.RadioCheckStatusColor
                .OrderBy(x => x.Name)
                .ToList();

            var redRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Red", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var greenRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Green", StringComparison.OrdinalIgnoreCase))
                .ToList();


            var orderedRecords = new List<RadioCheckStatusColor>();

            // Add blank record from DB (if it has an empty or specific Name like "")
            var blankRecord = filteredRecords.FirstOrDefault(x => string.IsNullOrEmpty(x.Name) || x.Name == "");
            if (blankRecord != null)
            {
                orderedRecords.Add(blankRecord);
            }

            orderedRecords.AddRange(redRecords);
            orderedRecords.AddRange(greenRecords);

            return orderedRecords;
        }
        public List<RadioCheckStatus> GetRadioCheckStatusWithOutcome()
        {
            var radiocheckstatus = _context.RadioCheckStatus.Include(x => x.RadioCheckStatusColor).ToList();
            /*
            foreach (var item in radiocheckstatus)
            {
                var radioCheckStatusColor = _context.RadioCheckStatusColor.Where(x => x.Id == item.RadioCheckStatusColorId).ToList();
                foreach (var item1 in radioCheckStatusColor)
                {
                    item.RadioCheckStatusColor.Name = item1.Name;
                }

            }
            */
            // return _context.RadioCheckStatus.ToList();
            return radiocheckstatus.OrderBy(x => Convert.ToInt32(x.ReferenceNo)).ToList();
        }
        public int GetRadioCheckStatusCount()
        {
            return _context.RadioCheckStatus.Count();
        }
        public List<SelectListItem> GetRadioCheckStatusForDropDown(bool withoutSelect = true, bool includeVisualPrefix = false)
        {
            var radioCheckStatuses = GetRadioCheckStatusWithOutcome();
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in radioCheckStatuses)
            {
                // Restore separators for empty names - 05-05-2024
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    items.Add(new SelectListItem("", item.Id.ToString()));
                    continue;
                }

                if (includeVisualPrefix)
                {
                    // Prepend color indicator and numeric part of Outcome (e.g. [🔴 1]) - 05-05-2024
                    string outcome = item.RadioCheckStatusColorName ?? "";
                    string dot = "⚪"; // Default white
                    string displayOutcome = outcome;

                    if (outcome.Contains("Red", StringComparison.OrdinalIgnoreCase))
                    {
                        dot = "🔴";
                        displayOutcome = outcome.Replace("Red", "", StringComparison.OrdinalIgnoreCase).Trim();
                    }
                    else if (outcome.Contains("Green", StringComparison.OrdinalIgnoreCase))
                    {
                        dot = "🟢";
                        displayOutcome = outcome.Replace("Green", "", StringComparison.OrdinalIgnoreCase).Trim();
                    }
                    else if (outcome.Contains("Yellow", StringComparison.OrdinalIgnoreCase))
                    {
                        dot = "🟡";
                        displayOutcome = outcome.Replace("Yellow", "", StringComparison.OrdinalIgnoreCase).Trim();
                    }

                    string formattedText = $"[{dot}{(string.IsNullOrWhiteSpace(displayOutcome) ? "" : " " + displayOutcome)}] {item.Name}";
                    items.Add(new SelectListItem(formattedText, item.Id.ToString()));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }

        //to get functions for settings in radio check-end
        //broadcast banner live events-start
        public List<BroadcastBannerLiveEvents> GetBroadcastLiveEvents()
        {
            return _context.BroadcastBannerLiveEvents.ToList();
        }
        public List<BroadcastBannerLiveEvents> GetBroadcastLiveEventsByDate()
        {
            return _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
        }
        public string GetBroadcastLiveEventsNotExpired()
        {
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = LiveEventstxtmsg + fileName.TextMessage.Replace("\n", "\t") + '\t' + '\t';
                }
            }
            return LiveEventstxtmsg;
        }
        public string GetBroadcastLiveEventsWeblink()
        {
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = fileName.Weblink;
                }
            }
            return LiveEventstxtmsg;
        }
        public string GetUrlsInsideBroadcastLiveEventsNotExpired()
        {
            string urls = string.Empty;
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = LiveEventstxtmsg + fileName.TextMessage.Replace("\n", "\t") + '\t' + '\t';
                }

                var lvsplit = LiveEventstxtmsg.Split(" ");

                foreach (var line in lvsplit)
                {
                    if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        urls = string.Concat(urls, line, "|");
                }
            }

            return urls;

        }

        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEventsByDate()
        {
            return _context.BroadcastBannerCalendarEvents.Where(x => DateTime.Now.Date >= x.StartDate && DateTime.Now.Date <= x.ExpiryDate).ToList();
        }
        //broadcast banner live events-end
        //broadcast banner calendar events-start
        public int GetCalendarEventsCount()
        {
            return _context.BroadcastBannerCalendarEvents.Count();
        }
        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEvents()
        {
            var calendarevents = _context.BroadcastBannerCalendarEvents.OrderBy(x => Convert.ToInt32(x.ReferenceNo.Replace("-PH", ""))).ToList();
            if (calendarevents.Count() > 0)
            {
                foreach (var item in calendarevents)
                {
                    string[] states = _context.PublicHolidayStates.Where(x => x.CalendarEventId == item.id && x.IsDeleted == false).Select(x => x.State).ToArray();
                    item.States = string.Join(",", states);
                    if (item.IsPublicHoliday)
                    {
                        if (item.ReferenceNo.EndsWith("-PH", StringComparison.OrdinalIgnoreCase))
                        {
                            item.ReferenceNo = item.ReferenceNo.Replace("-PH", "");
                        }
                        item.ReferenceNo = item.ReferenceNo + "-PH";
                    }
                }
            }

            return calendarevents;
        }
        //broadcast banner calendar events-end
        //SW Channels-start
        public List<SWChannels> GetSWChannels()
        {
            return _context.SWChannel.OrderBy(x => x.Id).ToList();
        }
        //SW Channels-end
        //General Feeds-start
        public List<GeneralFeeds> GetGeneralFeeds()
        {
            return _context.GeneralFeeds.OrderBy(x => x.Id).ToList();
        }
        //General Feeds-end
        public List<SmsChannel> GetSmsChannels()
        {
            return _context.SmsChannel.OrderBy(x => x.Id).ToList();
        }
        //p1-191 hr files task 3-start
        public List<HrSettings> GetHRSettings()
        {
            return _context.HrSettings.Where(x => x.IsDeleted == false).Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .Include(z => z.hrSettingsClientStates)
                .Include(z => z.hrSettingsClientSites)
                .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();
        }
        public List<HrSettings> GetHRSettingsUsingGroupId(int hrgroupId, string searchKeyNo)
        {



            var result = _context.HrSettings
       .Where(x => x.HRGroupId == hrgroupId && x.IsDeleted == false)
       .Include(z => z.HRGroups)
       .Include(x => x.ReferenceNoNumbers)
       .Include(x => x.ReferenceNoAlphabets)
       .OrderBy(x => x.Description)
        .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
        ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();

            //.Where(z => string.IsNullOrEmpty(searchKeyNo) || z.KeyNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))
            return result.Where(x => (string.IsNullOrEmpty(searchKeyNo) ||
                 x.Description.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase) ||
                 x.ReferenceNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))).ToList(); // Adding search by ReferenceNoAlphabets

        }


        public List<string> GetCompanyDetailsUsingFilter(int[] clientSiteIds, string searchKeyNo)
        {
            // Initialize the base query
            var query = _context.KeyVehicleLogs.AsQueryable();
            query = query.Where(z => z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog);

            // Apply the search key filter if provided
            if (!string.IsNullOrEmpty(searchKeyNo))
            {

                query = _context.KeyVehicleLogs.Where(k => string.IsNullOrEmpty(searchKeyNo) ||
                k.CompanyName.ToLower().Contains(searchKeyNo.ToLower()));
                //query = query.Where(z => z.CompanyName.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase));
            }

            // Apply the client site and log book type filters if client site IDs are provided
            if (clientSiteIds?.Length > 0)
            {
                query = query.Where(z => clientSiteIds.Contains(z.ClientSiteLogBook.ClientSiteId)
                                      && z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog);
            }

            // Execute the query and return the results
            // Select distinct company names and return as a list
            return query
                .Select(z => z.CompanyName) // Project only the company name
                .Distinct()
                 .OrderBy(name => name)  // Get distinct company names
                .ToList();
        }

        public List<HrSettings> GetHRSettingsWithHRLockEnable()
        {
            return _context.HrSettings.Where(x => x.HRLock == true).ToList();
        }
        public List<LicenseTypes> GetLicensesTypes()
        {
            return _context.LicenseTypes.Where(x => x.IsDeleted == false)
                .OrderBy(x => x.Id).ToList();
        }
        //p1-191 hr files task 3-end
        //p1-213 critical documents start
        public List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "")
        {
            if (guardId == 0)
            {
                var sites = new List<SelectListItem>();


                var mapping = _context.UserClientSiteAccess
               .Where(x => x.ClientSite.ClientType.Name.Trim() == type.Trim() && x.ClientSite.IsActive == true)
               .Include(x => x.ClientSite)
               .Include(x => x.ClientSite.ClientType)
               .Select(x => new { x.ClientSiteId, x.ClientSite.Name })
            .Distinct().
             OrderBy(x => x.Name).ToList();


                foreach (var item in mapping)
                {
                    sites.Add(new SelectListItem(item.Name, item.ClientSiteId.ToString()));
                }
                return sites;

            }
            else
            {
                var sites = new List<SelectListItem>();

                var mapping = _context.GuardLogins
                .Where(z => z.GuardId == guardId)
                    .Include(z => z.ClientSite)
                    .OrderBy(x => x.ClientSite.Name)
                    .ToList();


                foreach (var item in mapping)
                {
                    if (!sites.Any(cus => cus.Text == item.ClientSite.Name))
                    {
                        sites.Add(new SelectListItem(item.ClientSite.Name, item.ClientSite.Id.ToString()));
                    }
                }
                return sites;

            }
        }

        public List<SelectListItem> GetDescList(int HRGroupId)
        {
            var Group = new List<SelectListItem>();
            var descriptions = _context.HrSettings.Include(z => z.HRGroups)
       .Include(z => z.ReferenceNoNumbers)
       .Include(z => z.ReferenceNoAlphabets)
       .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
       ThenBy(x => x.ReferenceNoAlphabets.Name).Where(z => z.HRGroups.Id == HRGroupId && z.IsDeleted == false).ToList();
            var mapping = _context.HrSettings.Where(x => x.HRGroupId == HRGroupId).ToList();
            foreach (var item in descriptions)
            {
                Group.Add(new SelectListItem(item.ReferenceNo + "&nbsp;&nbsp;&nbsp;&nbsp;" + item.Description, item.Id.ToString()));

            }
            return Group;
        }
        public void RemoveCriticalDownSelect(CriticalDocuments CriticalDoc)
        {
            var Document = _context.KpiSendSchedules.FirstOrDefault(z => z.CriticalGroupNameID == CriticalDoc.Id.ToString());
            if (Document != null)
            {
                Document.IsCriticalDocumentDownselect = false;
                Document.CriticalGroupNameID = null;
                _context.SaveChanges();
            }
        }
        public void SaveCriticalDoc(CriticalDocuments CriticalDoc, bool updateClientSites1 = false)
        {
            var DesriptionDoc = _context.CriticalDocuments.Include(z => z.CriticalDocumentDescriptions).SingleOrDefault(z => z.Id == CriticalDoc.Id);
            var Document = _context.CriticalDocuments.Include(z => z.CriticalDocumentsClientSites)
                .Include(z => z.CriticalDocumentDescriptions)
                .SingleOrDefault(z => z.Id == CriticalDoc.Id);
            if (Document == null)
                _context.Add(CriticalDoc);
            else
            {
                if (updateClientSites1)
                {
                    _context.CriticalDocumentsClientSites.RemoveRange(Document.CriticalDocumentsClientSites);
                    _context.CriticalDocumentDescriptions.RemoveRange(DesriptionDoc.CriticalDocumentDescriptions);
                    _context.SaveChanges();
                }

                Document.HRGroupID = CriticalDoc.HRGroupID;
                Document.ClientTypeId = CriticalDoc.ClientTypeId;
                Document.GroupName = CriticalDoc.GroupName;
                Document.IsCriticalDocumentDownselect = CriticalDoc.IsCriticalDocumentDownselect;

                if (updateClientSites1)
                    Document.CriticalDocumentsClientSites = CriticalDoc.CriticalDocumentsClientSites;
                DesriptionDoc.CriticalDocumentDescriptions = CriticalDoc.CriticalDocumentDescriptions;
            }
            _context.SaveChanges();

        }
        public List<CriticalDocuments> GetCriticalDocs()
        {


            var document1 = _context.CriticalDocuments
    .Include(z => z.CriticalDocumentsClientSites)
        .ThenInclude(y => y.ClientSite)
            .ThenInclude(cs => cs.ClientType)
    .Include(z => z.CriticalDocumentDescriptions)
            .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.HRGroups)
     .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
        .ThenInclude(z => z.ReferenceNoNumbers)
         .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.ReferenceNoAlphabets)
     .Select(d => new
     {
         CriticalDocument = d,
         SortedDescriptions = d.CriticalDocumentDescriptions
            .Where(desc => desc.HRSettings != null && desc.HRSettings.ReferenceNoNumbers != null && desc.HRSettings.ReferenceNoAlphabets != null)
            .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
            .ThenBy(d => d.HRSettings.ReferenceNoAlphabets)
            .ToList()
     })
    .ToList();
            var sortedDocuments = document1.Select(doc =>
            {
                var criticalDocument = doc.CriticalDocument;
                criticalDocument.CriticalDocumentDescriptions = doc.SortedDescriptions;
                return criticalDocument;
            }).ToList();

            return sortedDocuments;


        }
        public CriticalDocuments GetCriticalDocById(int CriticalID)
        {
            var sss1 = _context.CriticalDocuments
   .Include(z => z.CriticalDocumentsClientSites)
       .ThenInclude(y => y.ClientSite)
           .ThenInclude(cs => cs.ClientType)
   .Include(z => z.CriticalDocumentDescriptions)
           .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.HRGroups)
    .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
       .ThenInclude(z => z.ReferenceNoNumbers)
        .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.ReferenceNoAlphabets)
  .SingleOrDefault(x => x.Id == CriticalID);

            var document = _context.CriticalDocuments
   .Include(z => z.CriticalDocumentsClientSites)
       .ThenInclude(y => y.ClientSite)
           .ThenInclude(cs => cs.ClientType)
   .Include(z => z.CriticalDocumentDescriptions)
           .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.HRGroups)
    .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
       .ThenInclude(z => z.ReferenceNoNumbers)
        .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.ReferenceNoAlphabets)
  .Where(x => x.Id == CriticalID)
    .Select(d => new
    {
        CriticalDocument = d,
        SortedDescriptions = d.CriticalDocumentDescriptions
            .Where(desc => desc.HRSettings != null && desc.HRSettings.ReferenceNoNumbers != null && desc.HRSettings.ReferenceNoAlphabets != null)
            .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
            .ThenBy(d => d.HRSettings.ReferenceNoAlphabets)
            .ToList()
    })
    .SingleOrDefault();

            if (document != null)
            {
                document.CriticalDocument.CriticalDocumentDescriptions = document.SortedDescriptions;
                return document.CriticalDocument;
            }

            return null;
        }


        public HrSettings GetHrSettingById(int CriticalID)
        {


            return _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .Include(z => z.hrSettingsClientStates)
                .Include(z => z.hrSettingsClientSites)
                .ThenInclude(y => y.ClientSite)
                .ThenInclude(y => y.ClientType)
                .OrderBy(z => z.HRGroups.Name).ThenBy(z => z.ReferenceNoNumbers.Name).
                ThenBy(z => z.ReferenceNoAlphabets.Name).SingleOrDefault(z => z.Id == CriticalID);


        }
                
        public CriticalDocuments GetCriticalDocByIdandGuardId(int CriticalID, int GuardId)
        {
            var distinctClientSiteIds = _context.GuardLogins
          .Where(z => z.GuardId == GuardId)
          .Select(z => z.ClientSite.Id)
          .Distinct()
          .ToList();
            var KpiSendSchedule = _context.CriticalDocuments
              .Include(z => z.CriticalDocumentsClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == CriticalID);
            foreach (var li in KpiSendSchedule.CriticalDocumentsClientSites)
            {
                if (!distinctClientSiteIds.Contains(li.ClientSiteId))
                {
                    KpiSendSchedule.CriticalDocumentsClientSites.Remove(li);

                }

            }
            return KpiSendSchedule;
        }
        public void DeleteCriticalDoc(int id)
        {
            var recordToDelete = _context.CriticalDocuments.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.CriticalDocuments.Remove(recordToDelete);
            _context.SaveChanges();
        }


        public List<StaffDocument> GetStaffDocumentModuleDetails()
        {

            return _context.StaffDocuments.Where(x => x.DocumentModuleName.Trim() != string.Empty).ToList();



        }

        public List<StaffDocument> GetStaffDocumentSOPDocDetails(int clientSiteId)
        {

            return _context.StaffDocuments.Where(x => x.DocumentType == 4 && x.ClientSite == clientSiteId && x.SOP != string.Empty).ToList();



        }


        public SubDomain GetSubDomainDetails(string domain)
        {

            return _context.SubDomain.Where(x => x.Domain.Trim() == domain.Trim()).FirstOrDefault();



        }
        public HandoverNotes GetHandoverNotes(int CientSiteID)
        {

            return _context.HandoverNotes.Where(x => x.ClientSiteID == CientSiteID).FirstOrDefault();
        }
        public SubDomain GetSubDomainID(int? TypeID)
        {

            return _context.SubDomain.Where(x => x.TypeId == TypeID).FirstOrDefault();



        }
        //p1-213 critical documents stop
        public string GetClientTypeNameById(int id)
        {

            return _context.ClientTypes.Where(x => x.Id == id).FirstOrDefault().Name;



        }

        public List<TrainingCourses> GetCourseDocsUsingSettingsId(int type)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourses
                .Include(x => x.TQNumber)
                .Where(x => x.HRSettingsId == type && x.IsDeleted == false)
                .OrderBy(x => x.TQNumberId)
                .ToList();
            foreach (var item in courseDocList)
            {
                item.TQNumberName = item.TQNumber.Name;
            }

            return courseDocList;
        }
        public List<TrainingTQNumbers> GetTQNumbers()
        {
            return _context.TrainingTQNumbers.OrderBy(x => x.Id).ToList();

        }

        public List<CriticalDocuments> GetCriticalDocsByClientSiteId(int clientSiteId)
        {
            var sortedDocuments = _context.CriticalDocuments
                .Include(d => d.CriticalDocumentsClientSites)
                    .ThenInclude(cs => cs.ClientSite)
                        .ThenInclude(ct => ct.ClientType)
                .Include(d => d.CriticalDocumentDescriptions)
                    .ThenInclude(desc => desc.HRSettings)
                        .ThenInclude(hr => hr.HRGroups)
                .Include(d => d.CriticalDocumentDescriptions)
                    .ThenInclude(desc => desc.HRSettings)
                        .ThenInclude(hr => hr.ReferenceNoNumbers)
                .Include(d => d.CriticalDocumentDescriptions)
                    .ThenInclude(desc => desc.HRSettings)
                        .ThenInclude(hr => hr.ReferenceNoAlphabets)
                .Where(d => d.CriticalDocumentsClientSites.Any(cs => cs.ClientSiteId == clientSiteId)) // Filter by ClientSiteId
                .Select(d => new
                {
                    CriticalDocument = d,
                    SortedDescriptions = d.CriticalDocumentDescriptions
                        .Where(desc => desc.HRSettings != null
                                    && desc.HRSettings.ReferenceNoNumbers != null
                                    && desc.HRSettings.ReferenceNoAlphabets != null)
                        .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
                        .ThenBy(desc => desc.HRSettings.ReferenceNoAlphabets)
                        .ToList(),
                    FilteredClientSites = d.CriticalDocumentsClientSites
                        .Where(cs => cs.ClientSiteId == clientSiteId) // Select only relevant ClientSites
                        .ToList()
                })
                .AsEnumerable() // Switch to client-side processing for modifications
                .Select(doc =>
                {
                    doc.CriticalDocument.CriticalDocumentDescriptions = doc.SortedDescriptions;
                    doc.CriticalDocument.CriticalDocumentsClientSites = doc.FilteredClientSites;
                    return doc.CriticalDocument;
                })
                .ToList();

            return sortedDocuments;

        }

        public List<int> GetCriticalDocDescriptionIdsForClientSiteIds(int[] clientSiteIds)
        {
            if (clientSiteIds == null || clientSiteIds.Length == 0)
                return new List<int>();

            return _context.CriticalDocumentDescriptions
                .Where(desc => desc.CriticalDocument.CriticalDocumentsClientSites.Any(cs => clientSiteIds.Contains(cs.ClientSiteId)))
                .Select(desc => desc.DescriptionID)
                .Distinct()
                .ToList();
        }

        public void SaveTrainingCourses(TrainingCourses trainingCourses)
        {
            if (trainingCourses.Id == 0)
            {
                _context.TrainingCourses.Add(trainingCourses);
            }
            else
            {
                var documentToUpdate = _context.TrainingCourses.SingleOrDefault(x => x.Id == trainingCourses.Id && x.IsDeleted == false);
                if (documentToUpdate != null)
                {
                    documentToUpdate.FileName = trainingCourses.FileName;
                    documentToUpdate.LastUpdated = trainingCourses.LastUpdated;
                    documentToUpdate.TQNumberId = trainingCourses.TQNumberId;
                    documentToUpdate.HRSettingsId = trainingCourses.HRSettingsId;
                    documentToUpdate.IsDeleted = trainingCourses.IsDeleted;
                }
            }
            _context.SaveChanges();
        }
        public int GetLastTQNumber(int hrsettingsid)
        {
            int LastTQNumber = 0;

            var result = _context.TrainingCourses.Where(x => x.HRSettingsId == hrsettingsid && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            if (result.Count == 0)
            {
                LastTQNumber = _context.TrainingTQNumbers.FirstOrDefault().Id;
            }
            if (result.Count > 0)
            {
                int[] tqnumbers = result.Select(x => x.TQNumberId).ToArray();
                LastTQNumber = _context.TrainingTQNumbers.Where(x => !tqnumbers.Contains(x.Id)).FirstOrDefault().Id;
            }
            return LastTQNumber;

        }
        public List<TrainingTestQuestionSettings> GetTQSettings(int hrSettingsId)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingTestQuestionSettings
                .Include(x => x.CourseDuration)
                .Include(x => x.TestDuration)
                .Include(x => x.PassMark)
                .Include(x => x.Attempts)
                .Include(x => x.CertificateExpiryYears)

                .Where(x => x.HRSettingsId == hrSettingsId && x.IsDeleted == false)
                .ToList();


            return courseDocList;
        }
        public int GetNextQuestionWithinSameTQNumber(int hrsettingsid, int tqNumberId)
        {
            int LastTQuestionNumber = 0;

            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            if (result.Count == 0)
            {
                LastTQuestionNumber = _context.TrainingTestQuestionNumbers.FirstOrDefault().Id;
            }
            if (result.Count > 0)
            {
                int[] questionnumbers = result.Select(x => x.QuestionNoId).ToArray();
                LastTQuestionNumber = _context.TrainingTestQuestionNumbers.Where(x => !questionnumbers.Contains(x.Id)).FirstOrDefault().Id;
            }
            return LastTQuestionNumber;

        }
        public int GetQuestionsCount(int hrsettingsid, int tqNumberId)
        {


            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();


            return result.Count;

        }
        public int GetLastTQNumberFromQuestions(int hrsettingsid)
        {


            int LastTQNumber = 0;

            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            if (result.Count > 0)
            {
                LastTQNumber = result.LastOrDefault().TQNumberId;
            }
            return LastTQNumber;

        }
        public TrainingTestQuestions GetTrainingQuestions(int hrsettingsid, int tqNumberId, int questionumberId)
        {



            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.TQNumberId == tqNumberId && x.QuestionNoId == questionumberId && x.IsDeleted == false).OrderBy(x => x.Id).FirstOrDefault();

            return result;

        }
        public List<TrainingTestQuestionsAnswers> GetTrainingQuestionsAnswers(int id)
        {



            var result = _context.TrainingTestQuestionsAnswers.Where(x => x.TrainingTestQuestionsId == id && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return result;

        }
        //public int GetLastFeedbackQNumbers(int hrsettingsid)
        //{

        //    int LastFeedbackQuestionNumber = 0;

        //    var result = _context.TrainingTestFeedbackQuestions.Where(x =>   x.IsDeleted == false).OrderBy(x => x.Id).ToList();
        //    if (result.Count == 0)
        //    {
        //        LastFeedbackQuestionNumber = _context.TrainingTestQuestionNumbers.FirstOrDefault().Id;
        //    }
        //    if (result.Count > 0)
        //    {
        //        int[] questionnumbers = result.Select(x => x.QuestionNoId).ToArray();
        //        LastFeedbackQuestionNumber = _context.TrainingTestQuestionNumbers.Where(x => !questionnumbers.Contains(x.Id)).FirstOrDefault().Id;
        //    }
        //    return LastFeedbackQuestionNumber;


        //}
        public int GetLastFeedbackQNumbers()
        {

            int LastFeedbackQuestionNumber = 0;

            var result = _context.TrainingTestFeedbackQuestions.Where(x => x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            if (result.Count == 0)
            {
                LastFeedbackQuestionNumber = _context.TrainingTestQuestionNumbers.FirstOrDefault().Id;
            }
            if (result.Count > 0)
            {
                int[] questionnumbers = result.Select(x => x.QuestionNoId).ToArray();
                LastFeedbackQuestionNumber = _context.TrainingTestQuestionNumbers.Where(x => !questionnumbers.Contains(x.Id)).FirstOrDefault().Id;
            }
            return LastFeedbackQuestionNumber;


        }
        public int GetFeedbackQuestionsCount()
        {


            var result = _context.TrainingTestFeedbackQuestions.Where(x => x.IsDeleted == false).OrderBy(x => x.Id).ToList();


            return result.Count;

        }
        //public TrainingTestFeedbackQuestions GetFeedbackQuestions(int hrsettingsid, int questionumberId)
        //{



        //    var result = _context.TrainingTestFeedbackQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.QuestionNoId == questionumberId && x.IsDeleted == false).OrderBy(x => x.Id).FirstOrDefault();

        //    return result;

        //}
        public TrainingTestFeedbackQuestions GetFeedbackQuestions(int questionumberId)
        {



            var result = _context.TrainingTestFeedbackQuestions.Where(x => x.QuestionNoId == questionumberId && x.IsDeleted == false).OrderBy(x => x.Id).FirstOrDefault();

            return result;

        }

        public List<TrainingTestFeedbackQuestionsAnswers> GetTrainingFeedbackQuestionsAnswers(int id)
        {



            var result = _context.TrainingTestFeedbackQuestionsAnswers.Where(x => x.TrainingTestFeedbackQuestionsId == id && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return result;

        }
        public List<TrainingCourses> GetCourseDocuments()
        {
            return _context.TrainingCourses.Where(x => x.IsDeleted == false).OrderBy(x => x.FileName).ToList();
        }
        public void DeleteCourseDocument(int id)
        {
            var docToDelete = _context.TrainingCourses.SingleOrDefault(x => x.Id == id && x.IsDeleted == false);
            if (docToDelete == null)
                throw new InvalidOperationException();

            docToDelete.IsDeleted = true;
            //_context.TrainingCourses.Remove(docToDelete);
            _context.SaveChanges();
        }


        public List<KPITelematicsField> GetTelematicsList()
        {
            return _context.KPITelematicsField.ToList();
        }
        public KPITelematicsField GetTelematicsMobileNo(int Id)
        {
            return _context.KPITelematicsField.Where(x => x.Id == Id).FirstOrDefault();
        }

        public ClientSite GetClientSiteLandline(int ClientSiteID)
        {
            return _context.ClientSites.Where(x => x.Id == ClientSiteID).FirstOrDefault();
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartwands(int ClientSiteID)
        {
            return _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == ClientSiteID && x.IsDeleted == false).ToList();
        }

        public List<TrainingCourseInstructor> GetCourseInstructor(int type)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourseInstructor
                .Where(x => x.HRSettingsId == type)
                .ToList();

            foreach (var item in courseDocList)
            {
                if (item.TrainingInstructorId == null)
                {
                    item.InstructorName = "";
                    item.InstructorPosition = "";

                }
                else
                {
                    item.InstructorName = _context.TrainingInstructor.Where(x => x.Id == item.TrainingInstructorId).FirstOrDefault().Name;
                    item.InstructorPosition = _context.TrainingInstructor.Where(x => x.Id == item.TrainingInstructorId).FirstOrDefault().Position;
                }
            }
            return courseDocList;
        }
        public void SaveTrainingCourseInstructor(TrainingCourseInstructor trainingCourseInstructor)
        {
            if (trainingCourseInstructor.Id == 0)
            {
                _context.TrainingCourseInstructor.Add(trainingCourseInstructor);
            }
            else
            {
                var documentToUpdate = _context.TrainingCourseInstructor.SingleOrDefault(x => x.Id == trainingCourseInstructor.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.HRSettingsId = trainingCourseInstructor.HRSettingsId;
                    documentToUpdate.TrainingInstructorId = trainingCourseInstructor.TrainingInstructorId;

                }
            }
            _context.SaveChanges();
        }
        public List<TrainingCourseCertificate> GetCourseCertificateDocsUsingSettingsId(int type)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourseCertificate
                .Where(x => x.HRSettingsId == type && x.IsDeleted == false)
                .ToList();


            return courseDocList;
        }
        public void SaveTrainingCourseCertificate(TrainingCourseCertificate trainingCourseCertificate)
        {
            if (trainingCourseCertificate.Id == 0)
            {
                _context.TrainingCourseCertificate.Add(trainingCourseCertificate);
            }
            else
            {
                var documentToUpdate = _context.TrainingCourseCertificate.SingleOrDefault(x => x.Id == trainingCourseCertificate.Id && x.IsDeleted == false);
                if (documentToUpdate != null)
                {
                    documentToUpdate.FileName = trainingCourseCertificate.FileName;
                    documentToUpdate.LastUpdated = trainingCourseCertificate.LastUpdated;
                    documentToUpdate.HRSettingsId = trainingCourseCertificate.HRSettingsId;
                    documentToUpdate.isRPLEnabled = trainingCourseCertificate.isRPLEnabled;
                    documentToUpdate.IsDeleted = trainingCourseCertificate.IsDeleted;
                }
            }
            _context.SaveChanges();
        }

        //p5-Issue-2-start
        //public List<TrainingCourses> GetTrainingCoursesStatusWithOutcome(int hrgroupid)
        //{

        //    var hrsettingsid = GetHRSettings().Where(x => x.HRGroupId == hrgroupid).Select(x => x.Id);
        //    //var trainigCourses = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => hrsettingsid.Contains(x.HRSettingsId)).ToList();

        //    var trainigCourses = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => hrsettingsid.Contains(x.HRSettingsId)
        //    && (_context.TrainingTestQuestionSettings.Any(tq => tq.HRSettingsId == x.HRSettingsId)
        //    && (_context.TrainingTestQuestions.Any(tq => tq.HRSettingsId == x.HRSettingsId))
        //    && (_context.TrainingCourseCertificate.Any(tq => tq.HRSettingsId == x.HRSettingsId)) &&
        //    (!_context.TrainingTestQuestionSettings
        //    .Where(tq => tq.HRSettingsId == x.HRSettingsId && tq.IsAnonymousFeedback)
        //    .Any() ||
        //    _context.TrainingTestFeedbackQuestions.Any(tfq => tfq.HRSettingsId == x.HRSettingsId)
        //)
        //    )
        //    ).ToList();

        //    // return _context.RadioCheckStatus.ToList();
        //    return trainigCourses.OrderBy(x => Convert.ToInt32(x.HRSettingsId)).ToList();
        //}
        public List<HrSettings> GetTrainingCoursesStatusWithOutcome(int hrgroupid)
        {

            var hrsettingsid = GetHRSettings().Where(x => x.HRGroupId == hrgroupid).Select(x => x.Id);
            var trainigCourses = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => hrsettingsid.Contains(x.HRSettingsId) && x.IsDeleted == false).ToList();

            //    var trainigCourses = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => hrsettingsid.Contains(x.HRSettingsId)
            //    && (_context.TrainingTestQuestionSettings.Any(tq => tq.HRSettingsId == x.HRSettingsId)
            //    && (_context.TrainingTestQuestions.Any(tq => tq.HRSettingsId == x.HRSettingsId))
            //    && (_context.TrainingCourseCertificate.Any(tq => tq.HRSettingsId == x.HRSettingsId)) &&
            //    (!_context.TrainingTestQuestionSettings
            //    .Where(tq => tq.HRSettingsId == x.HRSettingsId && tq.IsAnonymousFeedback)
            //    .Any() ||
            //    _context.TrainingTestFeedbackQuestions.Any(tfq => tfq.HRSettingsId == x.HRSettingsId)
            //)
            //    )
            //    ).ToList();

            // return _context.RadioCheckStatus.ToList();
            var trainingCoursesHRSettingsId = trainigCourses.Select(x => x.HRSettingsId);
            var hrsettings = GetHRSettings().Where(x => trainingCoursesHRSettingsId.Contains(x.Id)).ToList();
            //foreach(var item in hrsettings)
            //{
            //    var trainigCoursesnewresult = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => x.HRSettingsId == item.Id
            //    && (_context.TrainingTestQuestionSettings.Any(tq => tq.HRSettingsId == x.HRSettingsId)
            //    && (_context.TrainingTestQuestions.Any(tq => tq.HRSettingsId == x.HRSettingsId && tq.TQNumberId==x.TQNumberId))
            //    && (_context.TrainingCourseCertificate.Any(tq => tq.HRSettingsId == x.HRSettingsId)) &&
            //    (!_context.TrainingTestQuestionSettings
            //    .Where(tq => tq.HRSettingsId == x.HRSettingsId && tq.IsAnonymousFeedback)
            //    .Any() ||
            //    _context.TrainingTestFeedbackQuestions.Any(tfq => tfq.HRSettingsId == x.HRSettingsId)
            //     )
            //    )
            //    ).ToList();
            //    var trainigCoursesCount = _context.TrainingCourses.Include(x => x.TQNumber).Where(x => x.HRSettingsId==item.Id).ToList();
            //    if (trainigCoursesnewresult.Count()>0 && trainigCoursesCount.Count()== trainigCoursesnewresult.Count())
            //    {
            //        item.CourseStatus = "Green";
            //    }
            //    else
            //    {
            //        item.CourseStatus = "Yellow";
            //    }
            //}
            return hrsettings.OrderBy(x => Convert.ToInt32(x.Id)).ToList();
        }

        //p7-CourseLibraryPopupFix-start
        // WHY THIS NEW METHOD EXISTS (Course Library popup - selectCoursesForGuardByAdmin)
        //
        // The popup used to call GetTrainingCoursesStatusWithOutcome (just above). That method decides
        // "is this a course?" using ONE check only: does any non-deleted row exist in TrainingCourses.
        // That single check was wrong in BOTH directions and caused the 3 issues reported by the client:
        //
        //   a) "Employment Contract" appeared as a course.
        //      Uploading a CERTIFICATE against an HR record auto-creates a TrainingCourses row
        //      (Settings.cshtml.cs - OnPostUploadCourseCertificateDocUsingHR). That auto-created row
        //      stores the HR Description as the file name, so it has NO file extension. The old check
        //      accepted it, so an HR document became a "course".
        //
        //   b) "Level 3" went missing.
        //      Deleting/replacing the course document soft-deletes its only TrainingCourses row
        //      (DeleteCourseDocument), so the whole course vanished from the popup even though its
        //      certificate, instructor and test questions were all still set up.
        //
        //   c) The list was not A-Z.
        //      The old method ended with OrderBy(x => x.Id) - database record number, i.e. the order the
        //      HR records were created. That is why Level 5 was shown after Level X.
        //
        // The Admin screen "HR Groups - Course Library Only" (hr.cs - OnGetHRSettingsWithCourseLibrary)
        // already uses the correct rule and already shows the right list. So the popup now uses the SAME
        // rule as that Admin screen, and the two screens finally show the same thing:
        //   include the HR record if it has a course document WITH a real file extension,
        //   OR test questions, OR a course certificate, OR a course instructor.
        // Result: Employment Contract (0 of 4) drops out, Level 3 (certificate + instructor) comes back.
        //
        // NOTE: this is added as a SEPARATE method on purpose. GetTrainingCoursesStatusWithOutcome is
        // left completely untouched because _TrainingAndAssessmentSettings.cshtml still uses it, and we
        // must not change those other areas.
        public List<HrSettings> GetCourseLibraryHrSettings(int hrgroupid)
        {
            var hrSettingsInGroup = GetHRSettings().Where(x => x.HRGroupId == hrgroupid).ToList();

            // the same 4 checks used by the Admin "HR Groups - Course Library Only" screen
            var idsWithCourseDocs = GetCourseDocuments().Where(x => System.IO.Path.HasExtension(x.FileName)).Select(x => x.HRSettingsId);
            var idsWithQuestions = GetTrainingTestQuestions().Select(x => x.HRSettingsId);
            var idsWithCertificates = GetCourseCertificateDocuments().Select(x => x.HRSettingsId);
            var idsWithInstructors = GetCourseAllInstructor().Select(x => x.HRSettingsId);

            var courseLibraryIds = idsWithCourseDocs
                                   .Concat(idsWithQuestions)
                                   .Concat(idsWithCertificates)
                                   .Concat(idsWithInstructors)
                                   .Distinct()
                                   .ToList();

            // c) sorted A-Z by name instead of by database record number
            return hrSettingsInGroup.Where(x => courseLibraryIds.Contains(x.Id))
                                    .OrderBy(x => x.Description)
                                    .ToList();
        }
        //p7-CourseLibraryPopupFix-end

        public List<SelectListItem> GetHRGroupsDropDown(bool withoutSelect = true)
        {
            var hRGroups = _context.HRGroups.ToList();
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in hRGroups)
            {
                //items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        public void SaveGuardTrainingAndAssessmentTab(GuardTrainingAndAssessment trainingAssessment)
        {
            if (trainingAssessment.Id == 0)
            {
                _context.GuardTrainingAndAssessment.Add(trainingAssessment);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingAndAssessment.SingleOrDefault(x => x.Id == trainingAssessment.Id && x.TrainingCourseStatusId != 4);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = trainingAssessment.GuardId;
                    documentToUpdate.TrainingCourseId = trainingAssessment.TrainingCourseId;
                    documentToUpdate.Description = trainingAssessment.Description;
                    documentToUpdate.HRGroupId = trainingAssessment.HRGroupId;
                    documentToUpdate.TrainingCourseStatusId = trainingAssessment.TrainingCourseStatusId;
                    documentToUpdate.Attempts = trainingAssessment.Attempts;
                    //documentToUpdate.IsCompleted = trainingAssessment.IsCompleted;

                }
            }
            _context.SaveChanges();
        }

        public List<TrainingCourseCertificate> GetCourseCertificateDocuments()
        {
            return _context.TrainingCourseCertificate.Where(x => x.IsDeleted == false).OrderBy(x => x.FileName).ToList();
        }
        public void DeleteCourseCertificateDocument(int id)
        {
            var docToDelete = _context.TrainingCourseCertificate.SingleOrDefault(x => x.Id == id && x.IsDeleted == false);
            if (docToDelete == null)
                throw new InvalidOperationException();

            docToDelete.IsDeleted = true;
            //_context.TrainingCourseCertificate.Remove(docToDelete);
            _context.SaveChanges();
        }

        public StaffDocument GetStaffDocumentsID(int ClientSiteID)
        {
            return _context.StaffDocuments.Where(x => x.ClientSite == ClientSiteID && x.DocumentType == 6).FirstOrDefault();
        }
        //p5-Issue-2-end
        public List<TrainingCourseCertificateRPL> GetCourseCertificateRPLUsingId(int id)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourseCertificateRPL
                .Where(x => x.TrainingCourseCertificateId == id)
                .Include(x => x.TrainingLocation)
                .Include(x => x.TrainingTheoryLocation)
                .Include(x => x.TrainingInstructor)
                .ToList();


            return courseDocList;
        }
        public TrainingTestQuestions GetGuardQuestions(int hrSettingsId, int tqNumberId, int guardId)
        {
            var course = _context.TrainingCourses.Where(x => x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            int[] courseid = course.Select(x => x.Id).ToArray();
            var result = _context.GuardTrainingAttendedQuestionsAndAnswers.Where(x => courseid.Contains(x.TrainingCourseId) && x.GuardId == guardId).OrderBy(x => x.Id).ToList();
            var questions = new TrainingTestQuestions();
            if (result.Count == 0)
            {
                questions = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).FirstOrDefault();

            }
            if (result.Count > 0)
            {
                int[] questionnumbers = result.Select(x => x.TrainingTestQuestionsId).ToArray();
                questions = _context.TrainingTestQuestions.Where(x => !questionnumbers.Contains(x.Id) && x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).FirstOrDefault();

            }

            return questions;

        }
        public List<TrainingTestQuestionsAnswers> GetGuardOptions(int questionId)
        {
            var Options = _context.TrainingTestQuestionsAnswers.Where(x => x.TrainingTestQuestionsId == questionId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();


            return Options;

        }
        public void SaveGuardAnswers(GuardTrainingAttendedQuestionsAndAnswers attendedQuestions)
        {
            if (attendedQuestions.Id == 0)
            {
                _context.GuardTrainingAttendedQuestionsAndAnswers.Add(attendedQuestions);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingAttendedQuestionsAndAnswers.SingleOrDefault(x => x.Id == attendedQuestions.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = attendedQuestions.GuardId;
                    documentToUpdate.TrainingCourseId = attendedQuestions.TrainingCourseId;
                    documentToUpdate.TrainingTestQuestionsId = attendedQuestions.TrainingTestQuestionsId;
                    documentToUpdate.TrainingTestQuestionsAnswersId = attendedQuestions.TrainingTestQuestionsAnswersId;
                    documentToUpdate.IsCorrect = attendedQuestions.IsCorrect;

                }
            }
            _context.SaveChanges();
        }
        public int GetQuestionCount(int hrSettingsId, int tqNumberId)
        {
            var course = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return course.Count;

        }
        public List<GuardTrainingAttendedQuestionsAndAnswers> GetQuestionNumber(int hrSettingsId, int tqNumberId, int guardId)
        {
            var course = _context.TrainingCourses.Where(x => x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            int[] courseid = course.Select(x => x.Id).ToArray();
            var result = _context.GuardTrainingAttendedQuestionsAndAnswers.Where(x => courseid.Contains(x.TrainingCourseId) && x.GuardId == guardId).OrderBy(x => x.Id).ToList();
            return result;

        }
        public List<TrainingCourses> GetTrainingCourses(int hrSettingsId, int tqNumberId)
        {
            var course = _context.TrainingCourses.Where(x => x.HRSettingsId == hrSettingsId && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            return course;
        }
        public List<GuardTrainingAttendedQuestionsAndAnswers> GetGuardCorrectQuestions(int guardId, int trainingCourseId)
        {
            var correctQuestions = _context.GuardTrainingAttendedQuestionsAndAnswers.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId && x.IsCorrect == true).OrderBy(x => x.Id).ToList();
            return correctQuestions;
        }
        public void SaveGuardTestScores(GuardTrainingAndAssessmentScore scoreObtained)
        {
            if (scoreObtained.Id == 0)
            {
                _context.GuardTrainingAndAssessmentScore.Add(scoreObtained);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingAndAssessmentScore.SingleOrDefault(x => x.Id == scoreObtained.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = scoreObtained.GuardId;
                    documentToUpdate.TrainingCourseId = scoreObtained.TrainingCourseId;




                    documentToUpdate.TotalQuestions = scoreObtained.TotalQuestions;
                    documentToUpdate.guardCorrectQuestionsCount = scoreObtained.guardCorrectQuestionsCount;
                    documentToUpdate.guardScore = scoreObtained.guardScore;
                    documentToUpdate.IsPass = scoreObtained.IsPass;
                    documentToUpdate.duration = scoreObtained.duration;

                }
            }
            _context.SaveChanges();
        }
        public List<GuardTrainingAndAssessmentScore> GetGuardScores(int guardId, int trainingCourseId)
        {
            var scores = _context.GuardTrainingAndAssessmentScore.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId).OrderBy(x => x.Id).ToList();
            return scores;
        }
        public void DeleteGuardAttendedQuestions(int guardId, int trainingCourseId)
        {

            var report = _context.GuardTrainingAttendedQuestionsAndAnswers.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId && x.IsCorrect==false ).ToList(); ;
            if (report.Count > 0)
            {
                foreach (var item in report)
                {
                    _context.GuardTrainingAttendedQuestionsAndAnswers.Remove(item);
                }
            }
            _context.SaveChanges();
        }
        public void DeleteGuardScores(int guardId, int trainingCourseId)
        {

            var report = _context.GuardTrainingAndAssessmentScore.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId).ToList();
            if (report.Count > 0)
            {
                foreach (var item in report)
                {
                    _context.GuardTrainingAndAssessmentScore.Remove(item);
                }
            }
            _context.SaveChanges();
        }

        public GuardTrainingAndAssessment ReturnCourseTestStatusTostart(int guardId, int trainingCourseId)
        {

            //var report = _context.GuardTrainingAndAssessment.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId && x.IsCompleted==false).FirstOrDefault();
            var report = _context.GuardTrainingAndAssessment.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId && x.TrainingCourseStatusId != 4).FirstOrDefault();
            return report;
        }
        public void SaveGuardTrainingStartTest(GuardTrainingStartTest guardTrainingStartTest)
        {
            if (guardTrainingStartTest.Id == 0)
            {
                _context.GuardTrainingStartTest.Add(guardTrainingStartTest);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingStartTest.SingleOrDefault(x => x.Id == guardTrainingStartTest.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = guardTrainingStartTest.GuardId;
                    documentToUpdate.TrainingCourseId = guardTrainingStartTest.TrainingCourseId;
                    documentToUpdate.ClassroomLocationId = guardTrainingStartTest.ClassroomLocationId;
                    documentToUpdate.TestDate = guardTrainingStartTest.TestDate;

                }
            }
            _context.SaveChanges();
        }
        public List<GuardTrainingStartTest> GetGuardTrainingStartTest(int guardId, int trainingCourseId)
        {
            var startTest = _context.GuardTrainingStartTest.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId).
                Include(x => x.TrainingLocation)
                .OrderBy(x => x.Id).ToList();
            return startTest;
        }
        public List<TrainingCourses> GetTrainingCoursesWithHrSettingsId(int hrSettingsId)
        {
            var course = _context.TrainingCourses.Where(x => x.HRSettingsId == hrSettingsId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            return course;
        }
        public List<GuardTrainingAttendedQuestionsAndAnswers> GetGuardAttendedQuestionsAndanswers(int guardId, int trainingCourseId)
        {
            var correctQuestions = _context.GuardTrainingAttendedQuestionsAndAnswers.Where(x => x.GuardId == guardId && x.TrainingCourseId == trainingCourseId && x.IsCorrect == false).Include(x => x.TrainingTestQuestions).Include(x => x.TrainingTestQuestionsAnswers).OrderBy(x => x.Id).ToList();
            return correctQuestions;
        }
        public TrainingTestFeedbackQuestions GetGuardFeedbackQuestions(int hrSettingsId, int guardId)
        {

            var result = _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Where(x => x.HrSettingsId == hrSettingsId && x.GuardId == guardId).OrderBy(x => x.Id).ToList();
            var questions = new TrainingTestFeedbackQuestions();
            if (result.Count == 0)
            {
                //questions = _context.TrainingTestFeedbackQuestions.Where(x => x.HRSettingsId == hrSettingsId).FirstOrDefault();
                questions = _context.TrainingTestFeedbackQuestions.FirstOrDefault();

            }
            if (result.Count > 0)
            {
                int[] questionnumbers = result.Select(x => x.TrainingTestFeedbackQuestionsId).ToArray();
                //questions = _context.TrainingTestFeedbackQuestions.Where(x => !questionnumbers.Contains(x.Id) && x.HRSettingsId == hrSettingsId && x.IsDeleted == false).FirstOrDefault();
                questions = _context.TrainingTestFeedbackQuestions.Where(x => !questionnumbers.Contains(x.Id) && x.IsDeleted == false).FirstOrDefault();

            }

            return questions;

        }
        public List<TrainingTestFeedbackQuestionsAnswers> GetGuardFeedbackOptions(int questionId)
        {
            var Options = _context.TrainingTestFeedbackQuestionsAnswers.Where(x => x.TrainingTestFeedbackQuestionsId == questionId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();


            return Options;

        }
        //public int GetFeedbackQuestionCount(int hrSettingsId)
        //{
        //    var course = _context.TrainingTestFeedbackQuestions.Where(x => x.HRSettingsId == hrSettingsId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

        //    return course.Count;

        //}
        public int GetFeedbackQuestionCount()
        {
            var course = _context.TrainingTestFeedbackQuestions.Where(x => x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return course.Count;

        }
        public List<GuardTrainingAttendedFeedbackQuestionsAndAnswers> GetFeedbackQuestionNumber(int hrSettingsId, int guardId)
        {

            var result = _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Where(x => x.HrSettingsId == hrSettingsId && x.GuardId == guardId).OrderBy(x => x.Id).ToList();
            return result;

        }
        public void SaveGuardFeedbackAnswers(GuardTrainingAttendedFeedbackQuestionsAndAnswers attendedQuestions)
        {
            if (attendedQuestions.Id == 0)
            {
                _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Add(attendedQuestions);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.SingleOrDefault(x => x.Id == attendedQuestions.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = attendedQuestions.GuardId;
                    documentToUpdate.HrSettingsId = attendedQuestions.HrSettingsId;
                    documentToUpdate.TrainingTestFeedbackQuestionsId = attendedQuestions.TrainingTestFeedbackQuestionsId;
                    documentToUpdate.TrainingTestFeedbackQuestionsAnswersId = attendedQuestions.TrainingTestFeedbackQuestionsAnswersId;


                }
            }
            _context.SaveChanges();
        }

        public List<DuressAppField> GetDuressAppByType(int type, int? profileId = null)
        {
            var r = GetDuressAppFields()
                        .Where(x => x.TypeId == type &&
                                    (!profileId.HasValue || x.ProfileId == profileId))
                        .OrderBy(x => x.Label)
                        .ToList();

            foreach (var item in r)
            {
                if (item.ProfileId.HasValue)
                    item.Profile = _context.MobileLogActivityProfile.FirstOrDefault(x => x.Id == item.ProfileId);
            }

            return r;
        }



        // ✅ Insert New Record
        public bool AddDuressSetting(DuressSetting setting)
        {
            _context.DuressSettings.Add(setting);
            return _context.SaveChanges() > 0;
        }

        public DuressSetting GetDuressSetting(int clientSiteId, int siteDuressNumber)
        {
            return _context.DuressSettings
                .FirstOrDefault(d => d.ClientSiteId == clientSiteId && d.SiteDuressNumber == siteDuressNumber);
        }

        public DuressSetting GetDuressSettingById(int duressAppId)
        {
            return _context.DuressSettings.FirstOrDefault(d => d.Id == duressAppId);
        }
        public bool UpdateDuressSetting(DuressSetting setting)
        {
            _context.DuressSettings.Update(setting);
            return _context.SaveChanges() > 0;
        }

        public bool DeleteDuressSettingById(int duressAppId)
        {
            var duressSetting = _context.DuressSettings.FirstOrDefault(d => d.Id == duressAppId);
            if (duressSetting != null)
            {
                _context.DuressSettings.Remove(duressSetting);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId)
        {
            return _context.ClientSiteMobileAppSettings.AsNoTracking().FirstOrDefault(d => d.ClientSiteId == siteId);
        }

        public async Task<ClientSiteMobileCrowdControl> GetCrowdControlDataForSite(int siteId)
        {
            //ClientSiteMobileCrowdControl            
            return await _context.ClientSiteMobileCrowdControl.AsNoTracking().FirstOrDefaultAsync(d => d.ClientSiteId == siteId);
        }
        public async Task<List<ClientSiteMobileCrowdControlGuards>> GetCrowdControlLocationDataForSite(int siteId)
        {
            //ClientSiteMobileCrowdControlGuards
            return await _context.ClientSiteMobileCrowdControlGuards.AsNoTracking().Where(d => d.ClientSiteId == siteId).ToListAsync();
        }
        public async Task<List<ClientSiteMobileCrowdControlHistory>> GetCrowdControlHistoryDataForSite(int siteId)
        {
            //ClientSiteMobileCrowdControlHistory
            return await _context.ClientSiteMobileCrowdControlHistory.AsNoTracking().Where(d => d.ClientSiteId == siteId).ToListAsync();
        }

        public ClientSiteMobileAppSettings SaveCrowdSettingForSite(ClientSiteMobileAppSettings csmacs)
        {
            _context.Add(csmacs);
            _context.SaveChanges();
            return csmacs;
        }

        public ClientSiteMobileAppSettings UpdateCrowdSettingForSite(ClientSiteMobileAppSettings csmacs)
        {
            var existingRecord = _context.ClientSiteMobileAppSettings.FirstOrDefault(d => d.ClientSiteId == csmacs.ClientSiteId);
            existingRecord.IsCrowdCountEnabled = csmacs.IsCrowdCountEnabled;
            existingRecord.IsDoorEnabled = csmacs.IsDoorEnabled;
            existingRecord.IsGateEnabled = csmacs.IsGateEnabled;
            existingRecord.IsLevelFloorEnabled = csmacs.IsLevelFloorEnabled;
            existingRecord.IsRoomEnabled = csmacs.IsRoomEnabled;
            existingRecord.CounterQuantity = csmacs.CounterQuantity;
            _context.SaveChanges();
            return existingRecord;
        }

        public List<TrainingCourses> GetTrainingCoursesWithCourseId(int courseId)
        {
            var course = _context.TrainingCourses.Where(x => x.Id == courseId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            return course;
        }
        public void SaveGuardTrainingPracticalDetails(GuardTrainingAndAssessmentPractical trainingAssessment)
        {
            if (trainingAssessment.Id == 0)
            {
                _context.GuardTrainingAndAssessmentPractical.Add(trainingAssessment);
            }
            else
            {
                var documentToUpdate = _context.GuardTrainingAndAssessmentPractical.SingleOrDefault(x => x.Id == trainingAssessment.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.GuardId = trainingAssessment.GuardId;
                    documentToUpdate.HRSettingsId = trainingAssessment.HRSettingsId;
                    documentToUpdate.PracticalocationlId = trainingAssessment.PracticalocationlId;
                    documentToUpdate.PracticalDate = trainingAssessment.PracticalDate;
                    documentToUpdate.InstructorId = trainingAssessment.InstructorId;

                }
            }
            _context.SaveChanges();
        }
        public List<GuardTrainingAndAssessmentPractical> GetGuardTrainingPracticalDetails(int guardId, int hrsettingsId)
        {
            var startTest = _context.GuardTrainingAndAssessmentPractical.Where(x => x.GuardId == guardId && x.HRSettingsId == hrsettingsId).
                Include(x => x.TrainingLocation)
                .Include(x => x.TrainingInstructor)
                .OrderBy(x => x.Id).ToList();
            return startTest;
        }
        public List<TrainingCourses> GetTrainingCoursesWithOnlyHrSettingsId(int hrSettingsId)
        {
            var course = _context.TrainingCourses.Where(x => x.HRSettingsId == hrSettingsId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();
            return course;
        }

        public List<GuardTrainingAndAssessmentScore> GetallGuardAttendedCourse(int guardId)
        {
            var course = _context.GuardTrainingAndAssessmentScore.Where(x => x.GuardId == guardId).OrderBy(x => x.Id).ToList();
            return course;
        }
        public List<TrainingTestQuestions> GetTrainingQuestionsWithHRSettings(int hrsettingsid)
        {



            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return result;

        }
        public List<TrainingTestQuestions> GetTrainingQuestionsWithHRAndTQSettings(int hrsettingsid, int tqNumberId)
        {



            var result = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsid && x.TQNumberId == tqNumberId && x.IsDeleted == false).OrderBy(x => x.Id).ToList();

            return result;


        }
        public List<TrainingTestQuestions> GetTrainingTestQuestionsColor(int hrsettingsId)
        {

            //var trainigCourses = _context.TrainingTestQuestions.Include(x => x.TQNumbers).Where(x => x.HRSettingsId == hrsettingsId && x.IsDeleted == false
            var trainigCourses = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsId && x.IsDeleted == false

            //&& (_context.TrainingTestQuestionSettings.Any(tq => tq.HRSettingsId == x.HRSettingsId) 
            //&&
            //(!_context.TrainingTestQuestionSettings
            //.Where(tq => tq.HRSettingsId == x.HRSettingsId )
            //&& tq.IsAnonymousFeedback)
            //.Any() 
            //||
            //_context.TrainingTestFeedbackQuestions.Any(tfq => tfq.HRSettingsId == x.HRSettingsId && tfq.IsDeleted == false)

            //)
            //)
            ).ToList();

            return trainigCourses;

        }
        public List<GuardTrainingAttendedFeedbackQuestionsAndAnswers> GetGuardAttendedFeedBackQuestionsAndanswers(int guardId, int hrsettingsId)
        {
            var correctQuestions = _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Where(x => x.GuardId == guardId && x.HrSettingsId == hrsettingsId).Include(x => x.TrainingTestFeedbackQuestions).Include(x => x.TrainingTestFeedbackQuestionsAnswers).OrderBy(x => x.Id).ToList();
            return correctQuestions;
        }

        public async Task<List<ClientSiteMobileAppSettings>> GetAllCrowdControlSite()
        {
            return await _context.ClientSiteMobileAppSettings.AsNoTracking().Where(x => x.IsCrowdCountEnabled == true).ToListAsync();
        }

        public async Task<List<ClientSiteMobileCrowdControl>> GetAllCurrentCrowdControlData()
        {
            // Step 1: Get all enabled site IDs
            var enabledSiteIds = await _context.ClientSiteMobileAppSettings
                .AsNoTracking()
                .Where(x => x.IsCrowdCountEnabled == true)
                .Select(x => x.ClientSiteId)
                .ToListAsync();

            // Step 2: Get only crowd control data for enabled sites
            var result = await _context.ClientSiteMobileCrowdControl
                .AsNoTracking()
                .Where(x => enabledSiteIds.Contains(x.ClientSiteId))
                .ToListAsync();

            return result;
        }

        public async Task<List<ClientSiteMobileAppTimeZoneDTO>> GetClientSitesTimeZones()
        {
            var result = await _context.ClientSiteKpiSettings
                .Select(x => new ClientSiteMobileAppTimeZoneDTO
                {
                    ClientSiteId = x.ClientSiteId,
                    TimezoneString = x.TimezoneString,
                    UTC = x.UTC
                }).ToListAsync();

            return result;

        }

        public async Task SaveCrowdControlHistory(ClientSiteMobileCrowdControlHistory history)
        {
            _context.Add(history);
            await _context.SaveChangesAsync();
        }
        public async Task ResetSiteAndGuardCrowdControlData(ClientSiteMobileCrowdControl csmcc, DateTime ArchivedOn, string ArchivedMode)
        {
            var r = await _context.ClientSiteMobileCrowdControl.Where(x => x.Id == csmcc.Id).FirstOrDefaultAsync();
            if (r != null)
            {
                r.CrowdControlDate = csmcc.CrowdControlDate;
                r.Tcount = csmcc.Tcount;
                r.Ccount = csmcc.Ccount;
                r.LastUpdateTime = csmcc.LastUpdateTime;
            }
            else
            {
                _context.Add(csmcc);
            }

            //Check and reset each gaurd count
            var g = await _context.ClientSiteMobileCrowdControlGuards.Where(x => x.ClientSiteId == csmcc.ClientSiteId).ToListAsync();
            if (g != null)
            {
                List<ClientSiteMobileCrowdControlGuardsHistory> _csmccgh_list = new List<ClientSiteMobileCrowdControlGuardsHistory>();
                foreach (var c in g)
                {
                    //Create Guard count history

                    ClientSiteMobileCrowdControlGuardsHistory _csmccgh = new ClientSiteMobileCrowdControlGuardsHistory()
                    {
                        Id = c.Id,
                        CrowdControlId = c.CrowdControlId,
                        ClientSiteId = c.ClientSiteId,
                        UserId = c.UserId,
                        GuardId = c.GuardId,
                        BadgeNo = c.BadgeNo,
                        Pcount = c.Pcount,
                        Location = c.Location,
                        CrowdControlDate = c.CrowdControlDate,
                        GuardLastUpdateTime = c.GuardLastUpdateTime,
                        ArchivedOn = ArchivedOn,
                        ArchivedMode = ArchivedMode

                    };
                    _csmccgh_list.Add(_csmccgh);

                    c.CrowdControlDate = csmcc.CrowdControlDate;
                    c.GuardLastUpdateTime = csmcc.LastUpdateTime;
                    c.Pcount = 0;
                    c.Location = null;
                }

                if (_csmccgh_list != null && _csmccgh_list.Any())
                    _context.AddRange(_csmccgh_list);
            }

            await _context.SaveChangesAsync();
        }
        public void DeleteTQSettings(int id)
        {
            var docToDelete = _context.TrainingTestQuestionSettings.SingleOrDefault(x => x.Id == id && x.IsDeleted == false);
            if (docToDelete == null)
                throw new InvalidOperationException();

            docToDelete.IsDeleted = true;
            //_context.TrainingCourses.Remove(docToDelete);
            _context.SaveChanges();
        }
        public List<TrainingTestQuestions> GetTrainingTestQuestionsWithHrSettings(int hrsettingsId)
        {

            var trainigCourses = _context.TrainingTestQuestions.Where(x => x.HRSettingsId == hrsettingsId && x.IsDeleted == false).ToList();

            return trainigCourses;

        }
        public void DeleteGuardAttendedFeedbackQuestions(int guardId, int hrsettingsId)
        {

            var report = _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Where(x => x.GuardId == guardId && x.HrSettingsId == hrsettingsId).ToList(); ;
            if (report.Count > 0)
            {
                foreach (var item in report)
                {
                    _context.GuardTrainingAttendedFeedbackQuestionsAndAnswers.Remove(item);
                }
            }
            _context.SaveChanges();
        }
        public List<TrainingTestQuestions> GetTrainingTestQuestions()
        {


            var trainigCourses = _context.TrainingTestQuestions.Where(x => x.IsDeleted == false).ToList();

            return trainigCourses;

        }
        public List<TrainingCourseInstructor> GetCourseAllInstructor()
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourseInstructor
                .ToList();


            return courseDocList;
        }
        public List<SmartWandTagsType> GetSmartWandTagsType()
        {
            return _context.SmartWandTagsType.OrderBy(x => x.Id).ToList();

        }
        public List<RCActionListMessages> GetRCActionListMessages(int[] Ids)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.RCActionListMessages
                .Where(x => (x.IsDeleted == false)
                && ((Ids.Count() == 0) || (Ids.Contains(x.Id))))
                .ToList();
            foreach (var item in courseDocList)
            {
                if (item.Radiofrequencystatus == "EveryDay")
                {
                    item.ExpiryDate = item.Endmessagetime?.ToString("dd-MMM-yyyy  HH:mm:ss");
                    item.FQ = "E";
                }
                else
                {
                    item.ExpiryDate = item.messagetime?.ToString("dd-MMM-yyyy  HH:mm:ss");
                    item.FQ = "O";
                }

            }

            return courseDocList;
        }
        public List<RCActionListMessagesClientsites> GetRCActionListMessagesClientSites()
        {

            var clientSiteList = _context.RCActionListMessagesClientsites
                .Where(x => x.IsDeleted == false)
                .ToList();


            return clientSiteList;
        }

        public List<ClientSiteSmartWand> GetSmartWandList()
        {
            var smartWands = _context.ClientSiteSmartWands
                                     .Where(sw => sw.IsDeleted == false &&
                                                  _context.ClientSites
                                                          .Where(cs => cs.PatrolTourMode == PatrolTouringMode.PCAR)
                                                          .Select(cs => cs.Id)
                                                          .Contains(sw.ClientSiteId))
                                     .OrderBy(sw => sw.Id)
                                     .ToList();

            return smartWands;
        }


        public JsonResult GetPCARRouteDetails(int id)
        {
            var route = _context.PcarRoute
                .Include(r => r.RouteDetails)
                .ThenInclude(rd => rd.ClientSite)
                .ThenInclude(cs => cs.ClientType)
                .FirstOrDefault(r => r.Id == id);

            if (route == null) return new JsonResult(null);

            return new JsonResult(new
            {
                id = route.Id,
                pcarroutename = route.Pcarroutename,
                smartwandallocation = route.Smartwandallocation,
                routeDetails = route.RouteDetails.OrderBy(rd => rd.OrderNo).Select(rd => new
                {
                    clientSiteId = rd.ClientSiteId,
                    clientSiteName = rd.ClientSite.Name,
                    clientTypeName = rd.ClientSite.ClientType?.Name,
                    startMon = rd.StartMon,
                    endMon = rd.EndMon,
                    visitMon = rd.VisitMon,
                    startTue = rd.StartTue,
                    endTue = rd.EndTue,
                    visitTue = rd.VisitTue,
                    startWed = rd.StartWed,
                    endWed = rd.EndWed,
                    visitWed = rd.VisitWed,
                    startThu = rd.StartThu,
                    endThu = rd.EndThu,
                    visitThu = rd.VisitThu,
                    startFri = rd.StartFri,
                    endFri = rd.EndFri,
                    visitFri = rd.VisitFri,
                    startSat = rd.StartSat,
                    endSat = rd.EndSat,
                    visitSat = rd.VisitSat,
                    startSun = rd.StartSun,
                    endSun = rd.EndSun,
                    visitSun = rd.VisitSun,
                    startPho = rd.StartPho,
                    endPho = rd.EndPho,
                    visitPho = rd.VisitPho
                }).ToList()
            });
        }


        public bool RemovePCarDeatils(int PcarRouteId, List<int> siteIds)
        {
            var route = _context.PcarRoute
              .Include(r => r.RouteDetails)
              .FirstOrDefault(r => r.Id == PcarRouteId);

            if (route == null)
                return false;

            // Remove selected route details
            var detailsToRemove = route.RouteDetails.Where(rd => siteIds.Contains(rd.ClientSiteId)).ToList();
            _context.PcarRouteDetails.RemoveRange(detailsToRemove);
            _context.SaveChanges();
            return true;
        }
        public List<object> GetCriticalDocsUsingClientSiteIds(int[] clientSiteIds)
        {


            var document1 = _context.CriticalDocuments
    .Include(z => z.CriticalDocumentsClientSites)
        .ThenInclude(y => y.ClientSite)
            .ThenInclude(cs => cs.ClientType)
    .Include(z => z.CriticalDocumentDescriptions)
            .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.HRGroups)
     .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
        .ThenInclude(z => z.ReferenceNoNumbers)
         .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.ReferenceNoAlphabets)
     .Where(x => x.CriticalDocumentsClientSites
                                            .Any(cs => clientSiteIds.Contains(cs.ClientSiteId)))
     .Select(d => new
     {
         CriticalDocument = d,
         SortedDescriptions = d.CriticalDocumentDescriptions
            .Where(desc => desc.HRSettings != null && desc.HRSettings.ReferenceNoNumbers != null && desc.HRSettings.ReferenceNoAlphabets != null)
            .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
            .ThenBy(d => d.HRSettings.ReferenceNoAlphabets)
            .ToList()
     })
    .ToList();
            //var sortedDocuments = document1.Select(doc =>
            //{
            //    var criticalDocument = doc.CriticalDocument;
            //    criticalDocument.CriticalDocumentDescriptions = doc.SortedDescriptions;
            //    return criticalDocument;
            //}).ToList();
            var results = new List<object>();
            foreach (var item in document1)
            {
                foreach (var itemnew in item.SortedDescriptions)
                {
                    results.Add(new
                    {
                        Id=itemnew.HRSettings.Id.ToString(),

                        Description= itemnew.HRSettings.ReferenceNo + "&nbsp;&nbsp;&nbsp;&nbsp;" + itemnew.HRSettings.Description,
                        
                    });
                }
            }
          

            return results;


        }
        public void SaveOnboardUsersTrainingAndAssessmentTab(OnBoardUsersTrainingAndAssessment trainingAssessment)
        {
            if (trainingAssessment.Id == 0)
            {
                _context.OnBoardUsersTrainingAndAssessment.Add(trainingAssessment);
            }
            else
            {
                var documentToUpdate = _context.OnBoardUsersTrainingAndAssessment.SingleOrDefault(x => x.Id == trainingAssessment.Id && x.TrainingCourseStatusId != 4);
                if (documentToUpdate != null)
                {
                    documentToUpdate.UserId = trainingAssessment.UserId;
                    documentToUpdate.TrainingCourseId = trainingAssessment.TrainingCourseId;
                    documentToUpdate.Description = trainingAssessment.Description;
                    documentToUpdate.HRGroupId = trainingAssessment.HRGroupId;
                    documentToUpdate.TrainingCourseStatusId = trainingAssessment.TrainingCourseStatusId;
                    documentToUpdate.Attempts = trainingAssessment.Attempts;
                    //documentToUpdate.IsCompleted = trainingAssessment.IsCompleted;

                }
            }
            _context.SaveChanges();
        }
        public List<OnBoardUsersTrainingAndAssessment> GetOnBoardUsersTrainingAndAssessment(int userId)
        {
            return _context.OnBoardUsersTrainingAndAssessment.Where(x => x.UserId == userId).ToList();
        }

    }





}
