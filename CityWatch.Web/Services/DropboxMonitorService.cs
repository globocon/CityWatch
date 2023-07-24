using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Dropbox.Api;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface IDropboxMonitorService
    {
        Task CreateFolders();
    }

    public class DropboxMonitorService : IDropboxMonitorService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly ILogger<DropboxMonitorService> _logger;

        public DropboxMonitorService(IClientDataProvider clientDataProvider,
            IOptions<Settings> settings,
            ILogger<DropboxMonitorService> logger)
        {
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task CreateFolders()
        {
            _logger.LogInformation("DropboxMonitorService.CreateFolders started.");

            var clientTypeGroups = _clientDataProvider.GetClientSiteKpiSettings()
                                                .Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir))
                                                .GroupBy(z => z.ClientSite.TypeId);          
            try
            {
                using var dbxTeam = new DropboxTeamClient(_settings.DropboxAccessToken, _settings.DropboxRefreshToken, _settings.DropboxAppKey, _settings.DropboxAppSecret, new DropboxClientConfig());
                var team = await dbxTeam.Team.MembersListAsync();
                if (team.Members.Count > 0)
                {
                    var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == _settings.DropboxUserEmail);
                    if (cwsMember != null)
                    {
                        var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                        var account = await dbx.Users.GetCurrentAccountAsync();
                        var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                        foreach (var clientTypeGroup in clientTypeGroups)
                        {
                            foreach (var clientSite in clientTypeGroup.ToList())
                            {
                                await ProcessClientSite(dbx, nsId, clientSite);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
        }

        private async Task ProcessClientSite(DropboxClient dbx, PathRoot.NamespaceId nsId, ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var clientSiteDbxFolders = GetClientSiteDbxFolders(clientSiteKpiSetting).ToList();

            await CheckAndCreateFolders(dbx, nsId, clientSiteKpiSetting, clientSiteDbxFolders);

            _logger.LogInformation("DropboxMonitorService.ProcessClientSite for site {0} completed", clientSiteKpiSetting.ClientSite.Name);
        }

        private async Task CheckAndCreateFolders(DropboxClient dbx, PathRoot.NamespaceId nsId, ClientSiteKpiSetting clientSiteKpiSetting, IEnumerable<string> clientSiteDbxFolders)
        {
            var folderPathsToCreate = new List<string>();
            foreach (var folderPath in clientSiteDbxFolders)
            {
                try
                {
                    var isFolderExists = await CheckFolderExists(dbx, nsId, folderPath);
                    if (!isFolderExists)
                    {
                        folderPathsToCreate.Add(folderPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("DropboxMonitorService.CheckFolderExists for site {0} failed", clientSiteKpiSetting.ClientSite.Name);
                    _logger.LogError(ex.StackTrace);
                    break;
                }
            }

            if (folderPathsToCreate.Count > 0)
            {
                try
                {
                    await dbx.WithPathRoot(nsId).Files.CreateFolderBatchAsync(folderPathsToCreate);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("DropboxMonitorService.CreateFolderBatchAsync for site {0} failed", clientSiteKpiSetting.ClientSite.Name);
                    _logger.LogError(ex.StackTrace); ;
                }
            }
        }

        private static IEnumerable<string> GetClientSiteDbxFolders(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var clientSiteFolders = new List<string>();

            var siteBasePath = clientSiteKpiSetting.DropboxImagesDir;
            for (var dayIndex = 1; dayIndex <= 7; dayIndex++)
            {
                var targetDate = DateTime.Today.AddDays(dayIndex);

                var monthBasePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{targetDate.Year}/{targetDate:yyyyMM} - {targetDate.ToString("MMMM").ToUpper()} DATA";
                var dayPathFormat = clientSiteKpiSetting.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";

                clientSiteFolders.Add($"{monthBasePath}/x - Site KPI Telematics & Statistics");
                clientSiteFolders.Add($"{monthBasePath}/x - SmartWAND Patrol Reports");
                clientSiteFolders.Add($"{monthBasePath}/{targetDate.ToString(dayPathFormat).ToUpper()}/Daily Photos");
            }

            return clientSiteFolders.Distinct();
        }

        private static async Task<bool> CheckFolderExists(DropboxClient dbx, PathRoot.NamespaceId nsId, string folderPath)
        {
            try
            {
                var folderPathMeta = await dbx.WithPathRoot(nsId).Files.GetMetadataAsync(folderPath);
                if (folderPathMeta.IsFolder)
                    return true;
            }
            catch (ApiException<GetMetadataError> ex)
            {
                if (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound)
                {
                    return false;
                }

                throw ex;
            }

            return false;
        }
    }
}
