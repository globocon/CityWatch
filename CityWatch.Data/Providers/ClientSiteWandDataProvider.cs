using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteWandDataProvider
    {
        List<ClientSiteSmartWand> GetClientSiteSmartWands();
        List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms);
        void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        bool UpdateClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        bool DeRegisterDeviceWithSmartWand(int SmartWandId);
        void DeleteClientSiteSmartWand(int id);
        List<ClientSiteSmartWand> GetClientSiteAllSmartWands(int[] clientSiteIds);
        List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteAllSmartWandsStrikes(int[] clientSiteIds, DateTime fromDate, DateTime toDate);
        List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId);
        void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar);
        void DeleteClientSitePatrolCar(int id);
        ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id);
        void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag, WandTagChange changedBy = null);
        void DeleteClientSiteSmartWandTags(int id, WandTagChange changedBy = null);
        List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags();
        List<SmartWandTagsType> GetSmartWandTagsType();
        void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log);

        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds);
        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds);
        List<ClientSiteSmartWandTagsHitLog> GetClientSiteSmartWandTagsHitLogs(int[] clientSiteIds, DateTime fromDate, DateTime toDate);
        ClientSiteSmartWandTagsHitLog GetLastScannedTagDateTime(int siteId, string tagUid);
        List<SiteEquipmentsDetails> GetClientSiteEquipments();
        void SaveClientSiteEquipments(SiteEquipmentsDetails siteEquipmentsDetails);
        void DeleteClientSiteEquipments(int id);
        bool SaveOfflineSmartWandTagHitDataRecordError(ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced _offlineRecordsNotSynced);
        List<ClientSiteSmartWandTags> GetAllClientSitesSmartwandTags();
        public List<ClientSiteSmartWandTags> GetAllSmartwandTags();
        List<IncidentReportPosition> GetPatrolCars();
        List<IncidentReportPosition> GetPatrolCarsForSite(int[] clientsiteid);

        /// <summary>
        /// Tag History report: ClientSiteSmartWandTagsHist rows resolved to site, tag type, user and
        /// guard names, filtered and sorted as the request asks. No sites in the request means all.
        /// </summary>
        List<TagHistoryReportViewModel> GetTagHistoryReport(TagHistoryReportRequest request);

        /// <summary>Every tag UID that has history on the given sites, for the report's UID filter.</summary>
        List<string> GetTagHistoryTagUIds(int[] clientSiteIds);

    }

    public class ClientSiteWandDataProvider : IClientSiteWandDataProvider
    {
        private readonly CityWatchDbContext _dbContext;
        private readonly IConfigDataProvider _configDataProvider;

        public ClientSiteWandDataProvider(CityWatchDbContext dbContext, IConfigDataProvider configDataProvider)
        {
            _dbContext = dbContext;
            _configDataProvider = configDataProvider;
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands()
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .ToList();
        }
        public ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id)
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .Where(x => x.PhoneNumber == PhoneNumber && x.Id != id)
                .FirstOrDefault();
        }
        public List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms)
        {
            return _dbContext.ClientSiteSmartWands
                .Include(x => x.ClientSite)
                .Where(x => (string.IsNullOrEmpty(searchTerms) || x.PhoneNumber.Contains(searchTerms)) && x.ClientSite.IsActive == true && x.IsDeleted == false)
                .ToList();
        }

        public void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id == -1)
            {
                clientSiteSmartWand.Id = 0;
                clientSiteSmartWand.IsDeleted = false;
                _dbContext.ClientSiteSmartWands.Add(clientSiteSmartWand);
            }
            else
            {
                var clientSiteSmartWandToUpdate = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == clientSiteSmartWand.Id);
                if (clientSiteSmartWandToUpdate != null)
                {
                    clientSiteSmartWandToUpdate.SmartWandId = clientSiteSmartWand.SmartWandId;
                    clientSiteSmartWandToUpdate.PhoneNumber = clientSiteSmartWand.PhoneNumber;
                    clientSiteSmartWandToUpdate.SIMProvider = clientSiteSmartWand.SIMProvider;
                    clientSiteSmartWandToUpdate.IMEI = clientSiteSmartWand.IMEI;
                    clientSiteSmartWandToUpdate.PatrolCarId = clientSiteSmartWand.PatrolCarId;
                }
            }
            _dbContext.SaveChanges();
        }

        public bool UpdateClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id <= 0)            
                throw new Exception("Invalid Smart Wand.") ;
            
            _dbContext.SaveChanges();
            return true;                       
        }

        public bool DeRegisterDeviceWithSmartWand(int SmartWandId)
        {
            var smartWand = _dbContext.ClientSiteSmartWands.Where(x => x.Id == SmartWandId).FirstOrDefault();
            if (smartWand != null)
            {
                smartWand.DeviceId = null;
                smartWand.DeviceName = null;
                smartWand.DeviceType = null;
                try
                {
                    _dbContext.SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                throw new Exception("Smart Wand not found");
            }
        }

        public void DeleteClientSiteSmartWand(int id)
        {
            var deleteClientSiteSmartWand = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteSmartWand != null)
            {
                // Mark as deleted instead of removing it from the database
                deleteClientSiteSmartWand.IsDeleted = true;
            }
            else
            {
                throw new ArgumentException($"No Smart Wand found with ID: {id}");
            }

            _dbContext.SaveChanges();
        }

        public List<ClientSiteSmartWand> GetClientSiteAllSmartWands(int[] clientSiteIds)
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => clientSiteIds.Contains(x.ClientSiteId) && x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .ToList();
        }

        public List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteAllSmartWandsStrikes(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
        {
            toDate = toDate.AddDays(1); // Include the entire 'toDate' day

            return _dbContext.ClientSiteRadioChecksActivityStatus_History
                .Where(x => x.ClientSiteId.HasValue &&
                            clientSiteIds.Contains(x.ClientSiteId.Value) &&
                            x.ActivityType == "SW" &&
                            x.SwNotes != null &&
                            x.EventDateTime.Date >= fromDate.Date &&
                            x.EventDateTime.Date < toDate.Date)
                .Include(x => x.ClientSite)
                .OrderBy(x => x.EventDateTime)
                .ToList();
        }


        public List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId)
        {
            return _dbContext.ClientSitePatrolCars
                .Where(x => x.ClientSiteId == clientSiteId && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(x=> x.Model)
                .ToList();
        }

        public void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar)
        {
            if (clientSitePatrolCar.Id == -1)
            {
                clientSitePatrolCar.Id = 0;
                _dbContext.ClientSitePatrolCars.Add(clientSitePatrolCar);
            }
            else
            {
                var clientSitePatrolCarToUpdate = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == clientSitePatrolCar.Id);
                if (clientSitePatrolCarToUpdate != null)
                {
                    clientSitePatrolCarToUpdate.Model = clientSitePatrolCar.Model;
                    clientSitePatrolCarToUpdate.Rego = clientSitePatrolCar.Rego;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSitePatrolCar(int id)
        {
            var clientSitePatrolCarToDelete = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == id);
            if (clientSitePatrolCarToDelete != null)
            {
                _dbContext.ClientSitePatrolCars.Remove(clientSitePatrolCarToDelete);
                _dbContext.SaveChanges();
            }
        }
        public List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags()
        {
            var smartwandtags = _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }
        public List<SmartWandTagsType> GetSmartWandTagsType()
        {
            var smartwandtags = _configDataProvider.GetSmartWandTagsType();
            return smartwandtags;
        }

        public void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log)
        {
            if (log == null)
                throw new ArgumentNullException();

            _dbContext.ClientSiteSmartWandTagsHitLogs.Add(log);
            _dbContext.SaveChanges();
        }

        /// <param name="changedBy">
        /// Who is making the change and from which app, for the tag's change history. Optional:
        /// the tag still saves without it, the history row just has less to say about who did it.
        /// </param>
        public void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag,
            WandTagChange changedBy = null)
        {
            if (clientSiteSmartWandTag == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWandTag.TagsType == null)
                throw new ArgumentNullException("TagsType cannot be null. Please select a tag type.");

            clientSiteSmartWandTag.TagsTypeId = _dbContext.SmartWandTagsType.Where(x => x.value == clientSiteSmartWandTag.TagsType).FirstOrDefault().Id;
            var _existingTagUID = _dbContext.ClientSiteSmartWandTags.Where(x => x.UId == clientSiteSmartWandTag.UId
                && x.ClientSite.IsActive == true && x.IsDeleted == false).ToList();

            // What the tag looked like before this save, captured before anything is changed -
            // the history row is the difference between this and the tag afterwards.
            ClientSiteSmartWandTags before = null;
            ClientSiteSmartWandTags after;

            if (clientSiteSmartWandTag.Id <= 0)
            {
                clientSiteSmartWandTag.Id = 0;

                if (_existingTagUID.Any() || _existingTagUID.Count > 0)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                _dbContext.ClientSiteSmartWandTags.Add(clientSiteSmartWandTag);
                after = clientSiteSmartWandTag;
            }
            else
            {
                bool isTagExists = _dbContext.ClientSiteSmartWandTags.Any(x => x.UId == clientSiteSmartWandTag.UId
                        && x.Id != clientSiteSmartWandTag.Id && x.ClientSite.IsActive == true && x.IsDeleted == false);
                if (isTagExists)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                var clientSiteSmartWandTagToUpdate = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == clientSiteSmartWandTag.Id);
                after = clientSiteSmartWandTagToUpdate;
                if (clientSiteSmartWandTagToUpdate != null)
                {
                    before = SnapshotTag(clientSiteSmartWandTagToUpdate);

                    clientSiteSmartWandTagToUpdate.UId = clientSiteSmartWandTag.UId;
                    clientSiteSmartWandTagToUpdate.ClientSiteId = clientSiteSmartWandTag.ClientSiteId;
                    clientSiteSmartWandTagToUpdate.TagsTypeId = clientSiteSmartWandTag.TagsTypeId;
                    clientSiteSmartWandTagToUpdate.LabelDescription = clientSiteSmartWandTag.LabelDescription;
                    clientSiteSmartWandTagToUpdate.IsDeleted = false;
                    clientSiteSmartWandTagToUpdate.FqBypass = clientSiteSmartWandTag.FqBypass;

                }
            }

            _dbContext.SaveChanges();
            RecordTagChange(before, after, changedBy);
        }

        /// <param name="changedBy">See SaveClientSiteSmartWandTags.</param>
        public void DeleteClientSiteSmartWandTags(int id, WandTagChange changedBy = null)
        {
            var deleteClientSiteSmartWandTags = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == id);
            ClientSiteSmartWandTags before = null;
            if (deleteClientSiteSmartWandTags != null)
            {
                before = SnapshotTag(deleteClientSiteSmartWandTags);
                deleteClientSiteSmartWandTags.IsDeleted = true;
            }
            //_dbContext.ClientSiteSmartWandTags.Remove(deleteClientSiteSmartWandTags);

            _dbContext.SaveChanges();
            RecordTagChange(before, deleteClientSiteSmartWandTags, changedBy);
        }

        /// <summary>A detached copy of the values the history cares about, taken before a change.</summary>
        private static ClientSiteSmartWandTags SnapshotTag(ClientSiteSmartWandTags tag)
        {
            return new ClientSiteSmartWandTags
            {
                Id = tag.Id,
                ClientSiteId = tag.ClientSiteId,
                UId = tag.UId,
                TagsTypeId = tag.TagsTypeId,
                LabelDescription = tag.LabelDescription,
                IsDeleted = tag.IsDeleted,
                FqBypass = tag.FqBypass
            };
        }

        /// <summary>
        /// Writes one ClientSiteSmartWandTagsHist row for the change just saved.
        ///
        /// Deliberately after the tag's own SaveChanges rather than in it: the row needs the
        /// generated Id of a newly added tag, which only exists once that save has returned.
        ///
        /// Equally deliberately NOT inside an explicit transaction with it. CityWatch.Web
        /// registers the DbContext with EnableRetryOnFailure, and a retrying execution strategy
        /// refuses a user-started transaction - "The configured execution strategy 'SqlServer
        /// RetryingExecutionStrategy' does not support user-initiated transactions". Wrapping
        /// the two saves together would mean running the whole unit of work inside the strategy,
        /// and a replayed attempt cannot re-apply changes the tracker already thinks are saved.
        /// The cost of keeping them separate is that a process dying between the two loses one
        /// history row; the tag change itself is never at risk, which is the right way round.
        ///
        /// The history never costs a save, for the same reason: if this throws - most likely on
        /// a database where DbScript 382 has not been run and the table does not exist yet - the
        /// tag change stands and the failure is swallowed. The pending row is detached so it
        /// cannot resurface on someone else's later SaveChanges on this same scoped context.
        /// </summary>
        private void RecordTagChange(ClientSiteSmartWandTags before, ClientSiteSmartWandTags after,
            WandTagChange changedBy)
        {
            ClientSiteSmartWandTagsHist history = null;
            try
            {
                history = BuildTagChangeHistory(before, after, changedBy);
                if (history == null)
                    return;

                _dbContext.ClientSiteSmartWandTagsHist.Add(history);
                _dbContext.SaveChanges();
            }
            catch
            {
                if (history != null)
                {
                    try { _dbContext.Entry(history).State = EntityState.Detached; }
                    catch { /* nothing further to do - the tag change is already safe */ }
                }
            }
        }

        /// <summary>
        /// The history row for a change, or null when there is nothing worth recording - a save
        /// that rewrote identical values, or one that matched no tag at all.
        /// </summary>
        private ClientSiteSmartWandTagsHist BuildTagChangeHistory(ClientSiteSmartWandTags before,
            ClientSiteSmartWandTags after, WandTagChange changedBy)
        {
            if (after == null)
                return null;

            var added = before == null;
            if (!added && !TagChanged(before, after))
                return null;

            string action;
            if (added)
                action = "Added";
            else if (!before.IsDeleted && after.IsDeleted)
                action = "Deleted";
            else if (before.IsDeleted && !after.IsDeleted)
                action = "Restored";
            else
                action = "Changed";

            var userId = changedBy?.UserId ?? 0;
            var guardId = changedBy?.GuardId ?? 0;

            return new ClientSiteSmartWandTagsHist
            {
                TagId = after.Id,
                Action = action,
                ChangedOn = DateTime.Now,
                ChangedByUserId = userId > 0 ? userId : (int?)null,
                ChangedByGuardId = guardId > 0 ? guardId : (int?)null,
                ChangedBy = ResolveTagChangeActorName(userId, guardId),
                AppType = WandTagAppType.Normalise(changedBy?.AppType),
                AppVersion = ResolveTagChangeAppVersion(changedBy),
                Note = DescribeTagChange(before, after, action),

                OldClientSiteId = before?.ClientSiteId,
                NewClientSiteId = after.ClientSiteId,
                OldUId = before?.UId,
                NewUId = after.UId,
                OldTagsTypeId = before?.TagsTypeId,
                NewTagsTypeId = after.TagsTypeId,
                OldLabelDescription = before?.LabelDescription,
                NewLabelDescription = after.LabelDescription,
                OldIsDeleted = before?.IsDeleted,
                NewIsDeleted = after.IsDeleted,
                OldFqBypass = before?.FqBypass,
                NewFqBypass = after.FqBypass
            };
        }

        private static bool TagChanged(ClientSiteSmartWandTags before, ClientSiteSmartWandTags after)
        {
            return !string.Equals(before.UId ?? "", after.UId ?? "", StringComparison.Ordinal)
                || before.ClientSiteId != after.ClientSiteId
                || before.TagsTypeId != after.TagsTypeId
                || !string.Equals(before.LabelDescription ?? "", after.LabelDescription ?? "", StringComparison.Ordinal)
                || before.IsDeleted != after.IsDeleted
                || before.FqBypass != after.FqBypass;
        }

        /// <summary>
        /// The change in a sentence, for reading rather than querying - it says only what
        /// actually changed. The Old/New columns carry the same information for queries.
        /// </summary>
        private static string DescribeTagChange(ClientSiteSmartWandTags before,
            ClientSiteSmartWandTags after, string action)
        {
            var label = after.LabelDescription ?? "";
            var uid = after.UId ?? "";

            switch (action)
            {
                case "Added":
                    return $"Tag {uid} added to site {after.ClientSiteId} as '{label}'";
                case "Deleted":
                    return $"Tag {uid} ('{label}') marked deleted";
                case "Restored":
                    return $"Tag {uid} ('{label}') restored";
            }

            var parts = new List<string>();
            if (!string.Equals(before.UId ?? "", after.UId ?? "", StringComparison.Ordinal))
                parts.Add($"UID changed from '{before.UId}' to '{after.UId}'");
            if (before.ClientSiteId != after.ClientSiteId)
                parts.Add($"site changed from {before.ClientSiteId} to {after.ClientSiteId}");
            if (!string.Equals(before.LabelDescription ?? "", after.LabelDescription ?? "", StringComparison.Ordinal))
                parts.Add($"label changed from '{before.LabelDescription}' to '{after.LabelDescription}'");
            if (before.TagsTypeId != after.TagsTypeId)
                parts.Add($"tag type changed from {before.TagsTypeId} to {after.TagsTypeId}");
            if (before.FqBypass != after.FqBypass)
                parts.Add($"frequency bypass turned {(after.FqBypass ? "on" : "off")}");

            return string.Join("; ", parts);
        }

        /// <summary>
        /// The app version to record. The mobile app reports its build on login (see
        /// GuardMobileAppVersion), so when a tag change does not carry one we can still name the
        /// build from that - which means app versions released before this get attributed too,
        /// with no app change needed.
        /// </summary>
        private string ResolveTagChangeAppVersion(WandTagChange changedBy)
        {
            if (!string.IsNullOrWhiteSpace(changedBy?.AppVersion))
                return changedBy.AppVersion.Trim();

            var guardId = changedBy?.GuardId ?? 0;
            if (guardId <= 0)
                return null;

            return _dbContext.GuardMobileAppVersions.AsNoTracking()
                .Where(x => x.GuardId == guardId)
                .OrderByDescending(x => x.LastSeen)
                .Select(x => x.AppVersion)
                .FirstOrDefault();
        }

        private string ResolveTagChangeActorName(int changedByUserId, int changedByGuardId)
        {
            if (changedByGuardId > 0)
            {
                var guard = _dbContext.Guards.AsNoTracking()
                    .Where(x => x.Id == changedByGuardId)
                    .Select(x => x.Name).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(guard))
                    return $"{guard} (guard)";
            }

            if (changedByUserId > 0)
            {
                var user = _dbContext.Users.AsNoTracking()
                    .Where(x => x.Id == changedByUserId)
                    .Select(x => x.UserName).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(user))
                    return $"{user} (user)";
            }

            // Known id but no matching row, or no id at all. The ids themselves are still on the
            // history row; there is just no name to put beside them.
            return null;
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds)
        {
            return _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive && !x.IsDeleted && clientSiteIds.Contains(x.ClientSiteId))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite)
                .AsEnumerable() // switch to in-memory to allow property set
                .Select(x =>
                {
                    x.TagsType = x.SmartWandTagsType.value;
                    return x;
                })
                .ToList();
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds)
        {
            var logs = _dbContext.ClientSiteSmartWandTagsHitLogs
                .Where(x => clientSiteIds.Contains(x.LoggedInClientSiteId) ||
                            (x.TagLinkedClientSiteId.HasValue && clientSiteIds.Contains(x.TagLinkedClientSiteId.Value)))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.LoggedInClientSite)
                .Include(x => x.LinkedClientSite)
                .ToList();


            var tagList = logs.Select(x => new ClientSiteSmartWandTags
            {
                // Use TagLinkedClientSiteId if available, otherwise fall back to LoggedInClientSiteId
                ClientSiteId = x.TagLinkedClientSiteId ?? x.LoggedInClientSiteId,
                UId = x.TagUId,
                TagsTypeId = x.TagsTypeId ?? 0, // or handle null case as needed
                LabelDescription = x.LabelDescription,
                SmartWandTagsType = x.SmartWandTagsType,
                TagsType = x.SmartWandTagsType?.value,
                ClientSite = x.TagLinkedClientSiteId.HasValue ? x.LinkedClientSite : x.LoggedInClientSite,
                IsDeleted = false // assuming all from logs are "not deleted"
            })
            .GroupBy(x => new { x.ClientSiteId, x.UId }) // Optional: remove duplicates
            .Select(g => g.First()) // Keep one per UID per site
            .ToList();

            return tagList;
        }

        public List<ClientSiteSmartWandTagsHitLog> GetClientSiteSmartWandTagsHitLogs(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
        {
            toDate = toDate.AddDays(1); // Include the entire 'toDate' day

            // Step 1: Get UTC offsets per client site
            /* Looked up per site rather than used as the loop itself. While this drove the
               foreach, a site with no ClientSiteKpiSettings row produced no iteration at all,
               so its hit logs were never queried and the Wand Strikes report came back empty
               with no error - VISY-Yatala [VB] had 975 strikes hidden this way.
               Sites that do have a settings row behave exactly as before: the "+10:00"
               fallback below is what the old "?? +10:00" already yielded for a null UTC. */
            var siteUtcById = _dbContext.ClientSiteKpiSettings
                .Where(x => clientSiteIds.Contains(x.ClientSiteId))
                .Select(x => new { x.ClientSiteId, x.UTC })
                .ToList()
                .GroupBy(x => x.ClientSiteId)                  // GroupBy rather than ToDictionary:
                .ToDictionary(g => g.Key, g => g.First().UTC); // a duplicated site must not throw

            // Prepare a list to accumulate matching logs
            var matchingLogs = new List<ClientSiteSmartWandTagsHitLog>();

            foreach (var clientSiteId in clientSiteIds.Distinct())
            {
                // Step 2: Parse UTC offset like "+05:30" or "-04:00"
                var siteUtc = siteUtcById.TryGetValue(clientSiteId, out var utc) && !string.IsNullOrWhiteSpace(utc)
                    ? utc
                    : "+10:00"; // no settings row for this site, or the row has no UTC set
                if (!TimeSpan.TryParse(siteUtc.Replace("+", ""), out TimeSpan offset))
                {
                    // Default offset if parsing fails (you can customize this)
                    offset = TimeSpan.Zero;
                }

                // Step 3: Convert the local from/to to UTC for this site
                var fromUtc = fromDate - offset;
                var toUtc = toDate - offset;

                // Step 4: Get logs for this client site in the adjusted range
                var logs = _dbContext.ClientSiteSmartWandTagsHitLogs
                    .Where(x =>
                        (x.LoggedInClientSiteId == clientSiteId ||
                         (x.TagLinkedClientSiteId.HasValue && x.TagLinkedClientSiteId.Value == clientSiteId)) &&
                        x.HitUtcDateTime >= fromUtc &&
                        x.HitUtcDateTime <= toUtc)
                    .AsNoTracking()
                    .Include(x => x.SmartWandTagsType)
                    .Include(x => x.LoggedInClientSite)
                    .Include(x => x.LinkedClientSite)
                    .Include(x => x.LoggedInGuard)
                    .Include(x => x.LoggedInUser)
                    .ToList();

                var logsWithLocal = logs.Select(log => new ClientSiteSmartWandTagsHitLog
                {
                    Id = log.Id,
                    LoggedInClientSiteId = log.LoggedInClientSiteId,
                    LoggedInUserId = log.LoggedInUserId,
                    LoggedInGuardId = log.LoggedInGuardId,
                    TagUId = log.TagUId,
                    TagsTypeId = log.TagsTypeId,
                    LabelDescription = log.LabelDescription,
                    TagLinkedClientSiteId = log.TagLinkedClientSiteId,
                    HitUtcDateTime = log.HitUtcDateTime,
                    HitLocalDateTime = log.HitUtcDateTime + offset, // Add local time conversion
                    LoggedInClientSite = log.LoggedInClientSite,
                    LinkedClientSite = log.LinkedClientSite,
                    SmartWandTagsType = log.SmartWandTagsType,
                    SmartWandNameId = log.SmartWandNameId,
                    SmartWandId = log.SmartWandId,
                    LoggedInGuard = log.LoggedInGuard,
                    LoggedInUser = log.LoggedInUser,
                    GPScoordinates = log.GPScoordinates
                }).Where(l => l.HitLocalDateTime.Date >= fromDate.Date && l.HitLocalDateTime.Date < toDate.Date).ToList();

                matchingLogs.AddRange(logsWithLocal);
            }

            return matchingLogs;
        }

        public ClientSiteSmartWandTagsHitLog GetLastScannedTagDateTime(int siteId,string tagUid)
        {
            return _dbContext.ClientSiteSmartWandTagsHitLogs
                .Where(x => x.TagUId == tagUid && x.LoggedInClientSiteId == siteId)
                .OrderByDescending(x => x.HitUtcDateTime)
                .Take(1)
                .SingleOrDefault();
        }
        public List<SiteEquipmentsDetails> GetClientSiteEquipments()
        {
            var siteEquipments = _dbContext.SiteEquipmentsDetails
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.KPITelematicsField)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
            foreach (var item in siteEquipments)
            {
                item.Equipment = item.KPITelematicsField.Name;
            }
            return siteEquipments;
        }
        public void SaveClientSiteEquipments(SiteEquipmentsDetails siteEquipmentsDetails)
        {
            if (siteEquipmentsDetails == null)
                throw new ArgumentNullException();

          
            if (siteEquipmentsDetails.Id == 0)
            {
               

                
                _dbContext.SiteEquipmentsDetails.Add(siteEquipmentsDetails);
            }
            else
            {
               
                var clientSiteEquipmentTagToUpdate = _dbContext.SiteEquipmentsDetails.SingleOrDefault(x => x.Id == siteEquipmentsDetails.Id);
                if (clientSiteEquipmentTagToUpdate != null)
                {
                    clientSiteEquipmentTagToUpdate.Brand = siteEquipmentsDetails.Brand;
                    clientSiteEquipmentTagToUpdate.ClientSiteId = siteEquipmentsDetails.ClientSiteId;
                    clientSiteEquipmentTagToUpdate.EquipmentId = siteEquipmentsDetails.EquipmentId;
                    clientSiteEquipmentTagToUpdate.SerialNo = siteEquipmentsDetails.SerialNo;
                    clientSiteEquipmentTagToUpdate.IsDeleted = false;

                }
            }
            _dbContext.SaveChanges();
        }
        public void DeleteClientSiteEquipments(int id)
        {
            var deleteClientSiteEquipments = _dbContext.SiteEquipmentsDetails.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteEquipments != null)
                deleteClientSiteEquipments.IsDeleted = true;
            //_dbContext.ClientSiteSmartWandTags.Remove(deleteClientSiteSmartWandTags);

            _dbContext.SaveChanges();
        }

        public bool SaveOfflineSmartWandTagHitDataRecordError(ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced _offlineRecordsNotSynced)
        {
            try
            {
                _dbContext.ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced.Add(_offlineRecordsNotSynced);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public List<ClientSiteSmartWandTags> GetAllClientSitesSmartwandTags()
        {
                       var smartwandtags = _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSite)
                .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }

        public List<ClientSiteSmartWandTags> GetAllSmartwandTags()
        {
            var smartwandtags = _dbContext.ClientSiteSmartWandTags
             .Include(x => x.SmartWandTagsType)
             .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }

        public List<IncidentReportPosition> GetPatrolCars()
        {
            var PatrolCars = _dbContext.IncidentReportPositions.Where(x => x.IsPatrolCar == true).ToList();
            return PatrolCars;
        }

        public List<IncidentReportPosition> GetPatrolCarsForSite(int[] clientsiteid)
        {            
            var PatrolCarIds = _dbContext.ClientSiteSmartWands
               .Where(x => clientsiteid.Contains(x.ClientSiteId) && x.ClientSite.IsActive == true && x.IsDeleted == false && x.PatrolCarId != null)
               .Include(x => x.ClientSite)
               .Select(x => x.PatrolCarId)
               .Distinct()
               .ToArray();

            return _dbContext.IncidentReportPositions.Where(x => PatrolCarIds.Contains(x.Id)).ToList();

        }
    
        public List<TagHistoryReportViewModel> GetTagHistoryReport(TagHistoryReportRequest request)
        {
            var fromDate = request.LogFromDate.Date;
            var toDate = request.LogToDate.Date.AddDays(1);
            var clientSiteIds = request.ClientSiteIds;
            var tagUIds = request.TagUIds;
            var tagTypeIds = request.TagTypeIds;
            var guardName = request.GuardName?.Trim();
            var guardSecurityNo = request.GuardSecurityNo?.Trim();
            var userName = request.UserName?.Trim();

            // The tag's site, UID and type after the change; Old* only matters if New* is missing.
            // Joined rather than navigated because the history deliberately has no foreign keys.
            var query = from h in _dbContext.ClientSiteSmartWandTagsHist.AsNoTracking()
                        where h.ChangedOn >= fromDate && h.ChangedOn < toDate
                        join s in _dbContext.ClientSites on (h.NewClientSiteId ?? h.OldClientSiteId) equals (int?)s.Id into sj
                        from s in sj.DefaultIfEmpty()
                        join ct in _dbContext.ClientTypes on (int?)s.TypeId equals (int?)ct.Id into ctj
                        from ct in ctj.DefaultIfEmpty()
                        join t in _dbContext.SmartWandTagsType on (h.NewTagsTypeId ?? h.OldTagsTypeId) equals (int?)t.Id into tj
                        from t in tj.DefaultIfEmpty()
                        join u in _dbContext.Users on h.ChangedByUserId equals (int?)u.Id into uj
                        from u in uj.DefaultIfEmpty()
                        join g in _dbContext.Guards on h.ChangedByGuardId equals (int?)g.Id into gj
                        from g in gj.DefaultIfEmpty()
                        select new TagHistoryReportViewModel
                        {
                            Id = h.Id,
                            TagId = h.TagId,
                            ClientSiteId = h.NewClientSiteId ?? h.OldClientSiteId,
                            SiteName = s.Name,
                            ClientTypeName = ct.Name,
                            Action = h.Action,
                            Note = h.Note,
                            TagUId = h.NewUId ?? h.OldUId,
                            TagTypeId = h.NewTagsTypeId ?? h.OldTagsTypeId,
                            TagType = t.value,
                            LabelDescription = h.NewLabelDescription ?? h.OldLabelDescription,
                            ChangedByUserId = h.ChangedByUserId,
                            ChangedByGuardId = h.ChangedByGuardId,
                            UserName = u.UserName,
                            GuardName = g.Name,
                            GuardInitial = g.Initial,
                            GuardSecurityNo = g.SecurityNo,
                            ChangedBy = h.ChangedBy,
                            AppType = h.AppType,
                            AppVersion = h.AppVersion,
                            ChangedOn = h.ChangedOn
                        };

            if (clientSiteIds.Length > 0)
                query = query.Where(x => x.ClientSiteId.HasValue && clientSiteIds.Contains(x.ClientSiteId.Value));

            if (tagUIds.Length > 0)
                query = query.Where(x => tagUIds.Contains(x.TagUId));

            if (tagTypeIds.Length > 0)
                query = query.Where(x => x.TagTypeId.HasValue && tagTypeIds.Contains(x.TagTypeId.Value));

            if (!string.IsNullOrEmpty(guardName))
                query = query.Where(x => x.GuardName.Contains(guardName));

            if (!string.IsNullOrEmpty(guardSecurityNo))
                query = query.Where(x => x.GuardSecurityNo.Contains(guardSecurityNo));

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(x => x.UserName.Contains(userName) || (x.ChangedByUserId == null && x.ChangedByGuardId == null && x.ChangedBy.Contains(userName)));

            return SortTagHistory(query.ToList(), request.SortBy, request.SortDescending);
        }

        /// <summary>
        /// Orders the report by the chosen column, then by date/time and id so rows that tie on
        /// the column stay in the order they happened. Unknown or blank sort keys mean Date Time.
        /// </summary>
        public static List<TagHistoryReportViewModel> SortTagHistory(List<TagHistoryReportViewModel> rows, string sortBy, bool descending)
        {
            Func<TagHistoryReportViewModel, string> key = sortBy switch
            {
                TagHistorySortBy.Site => x => x.SiteName ?? string.Empty,
                TagHistorySortBy.TagUId => x => x.TagUId ?? string.Empty,
                TagHistorySortBy.TagType => x => x.TagType ?? string.Empty,
                TagHistorySortBy.Action => x => x.Action ?? string.Empty,
                TagHistorySortBy.User => x => x.UserDisplay ?? string.Empty,
                TagHistorySortBy.Guard => x => x.GuardName ?? string.Empty,
                _ => null
            };

            IOrderedEnumerable<TagHistoryReportViewModel> ordered;
            if (key == null)
            {
                ordered = descending
                    ? rows.OrderByDescending(x => x.ChangedOn).ThenByDescending(x => x.Id)
                    : rows.OrderBy(x => x.ChangedOn).ThenBy(x => x.Id);
            }
            else
            {
                ordered = (descending
                        ? rows.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
                        : rows.OrderBy(key, StringComparer.OrdinalIgnoreCase))
                    .ThenBy(x => x.ChangedOn)
                    .ThenBy(x => x.Id);
            }

            return ordered.ToList();
        }

        public List<string> GetTagHistoryTagUIds(int[] clientSiteIds)
        {
            var query = _dbContext.ClientSiteSmartWandTagsHist.AsNoTracking();
            if (clientSiteIds != null && clientSiteIds.Length > 0)
                query = query.Where(h => clientSiteIds.Contains(h.NewClientSiteId ?? h.OldClientSiteId ?? 0));

            return query.Select(h => h.NewUId ?? h.OldUId)
                .Where(x => x != null && x != "")
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }
}
