using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Pages.Guard;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using CityWatch.RadioCheck.Helpers;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using static System.Net.WebRequestMethods;
using System.Linq;

namespace CityWatch.Web.Pages
{
    public class RecordModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IDropboxService _dropboxUploadService;
        private readonly Settings _settings;
        private readonly IConfiguration _configuration;
        public RecordModel(
            IClientDataProvider clientDataProvider,
            IWebHostEnvironment webHostEnvironment, 
            IGuardDataProvider guardDataProvider,
            IOptions<Settings> settings,
            IDropboxService dropboxUploadService,
            IConfiguration configuration)
        {
            _clientDataProvider = clientDataProvider;
            _WebHostEnvironment = webHostEnvironment;
            _guardDataProvider = guardDataProvider;
            _settings = settings.Value;
            _dropboxUploadService = dropboxUploadService;
            _configuration = configuration;
        }
        public void OnGet()
        {
        }

        /// <param name="clientLocalDateTime">Recording start in the control room's local time ("yyyy-MM-dd HH:mm:ss"), sent by recorder.js</param>
        /// <param name="recordingSessionId">Groups the parts of one recording that was split by page reloads</param>
        /// <param name="partNo">Part number within the recording session</param>
        /// <param name="guardId">Guard who started the recording, sent by recorder.js (falls back to the session guard)</param>
        public async Task<IActionResult> OnPostSaveAudioAsync(IFormFile audioFile, string clientLocalDateTime, string recordingSessionId, int? partNo, int? guardId)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                // A part cut off by a page reload can be empty - nothing to save, not an error
                return new JsonResult(new { success = false, message = "No file received or file is empty." });
            }

            // Use the browser's local time; the server's clock may be in a different timezone
            var recordingDateTime = ParseClientLocalDateTime(clientLocalDateTime) ?? DateTime.Now;

            // Generate a unique file name
            var uniqueFileName = GenerateUniqueFileName(audioFile.FileName, recordingDateTime, GetGuardSecurityNo(guardId), recordingSessionId, partNo);

            // Define the folder path for audio recording
            var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", recordingDateTime.ToString("dd_MM_yyyy"));

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            // Save the file to the server
            var filePath = Path.Combine(folderPath, uniqueFileName);
            await SaveFileAsync(audioFile, filePath);

            
            // Upload the file to Dropbox and Azure bob
            var contentType = string.IsNullOrEmpty(audioFile.ContentType) ? "audio/webm" : audioFile.ContentType;
            var blobUrl= blobilpaod(filePath, recordingDateTime, contentType);
          

            var clientSite = _clientDataProvider.GetClientSiteForRcLogBook();
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSite.FirstOrDefault().Id);
            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;

            var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            var recordingDate = recordingDateTime.Date;
            var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{recordingDate.Year}/{recordingDate:yyyyMM} - {recordingDate.ToString("MMMM").ToUpper()} DATA/{recordingDate.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(filePath);


            var isUploaded = await UploadFileToDropboxAsync(filePath, dbxFilePath);
            if (isUploaded)
            {
                _guardDataProvider.SaveRecordingFileDetails(new AudioRecordingLog { BlobUrl = blobUrl, FileName = uniqueFileName, DropboxPath= dbxFilePath });
            }
            // Return the result with the unique file name
            return new JsonResult(new { success = isUploaded, fileName = uniqueFileName });
        }

        private static DateTime? ParseClientLocalDateTime(string clientLocalDateTime)
        {
            if (DateTime.TryParseExact(clientLocalDateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
            return null;
        }

        // SecurityNo of the guard who started the recording (posted guardId), else of the guard
        // logged in to RC (session "GuardId", set by RadioCheckV2 login), else NOGUARD
        private string GetGuardSecurityNo(int? postedGuardId)
        {
            var securityNo = FindSecurityNo(postedGuardId ?? 0)
                ?? FindSecurityNo(HttpContext.Session.GetInt32("GuardId") ?? 0);
            return securityNo ?? "NOGUARD";
        }

        private string FindSecurityNo(int guardId)
        {
            if (guardId <= 0)
                return null;

            var securityNo = _guardDataProvider.GetGuardDetailsUsingId(guardId).FirstOrDefault()?.SecurityNo;
            // Licence numbers can contain spaces or slashes - keep the file name / Dropbox path safe
            var safeSecurityNo = new string((securityNo ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            return string.IsNullOrEmpty(safeSecurityNo) ? null : safeSecurityNo;
        }

        private string GenerateUniqueFileName(string originalFileName, DateTime recordingDateTime, string guardSecurityNo, string recordingSessionId, int? partNo)
        {
            var ticks = DateTime.UtcNow.Ticks;
            var fileExtension = Path.GetExtension(originalFileName);
            var formattedDate = recordingDateTime.ToString("dd_MM_yyyy");

            // Parts of a recording split by page reloads share the session id: ..._S<session>_P<part>
            var sessionPart = string.Empty;
            var safeSessionId = new string((recordingSessionId ?? string.Empty).Where(char.IsLetterOrDigit).Take(32).ToArray());
            if (!string.IsNullOrEmpty(safeSessionId))
                sessionPart = $"_S{safeSessionId}_P{partNo ?? 1}";

            return $"recording_{formattedDate}_{guardSecurityNo}_{ticks}{sessionPart}{fileExtension}";
        }

        private async Task SaveFileAsync(IFormFile audioFile, string filePath)
        {
            await using var stream = new FileStream(filePath, FileMode.Create);
            await audioFile.CopyToAsync(stream);
        }

        private async Task<bool> UploadFileToDropboxAsync(string localFilePath,string dropBoxPath)
        {
            try
            {
                //var formattedDate = DateTime.Today.ToString("dd_MM_yyyy");
                //var fileName = Path.GetFileName(localFilePath);
                //var dropboxDir = _guardDataProvider.GetDrobox();

                
                //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{dropboxDir.DropboxDir}/RCAudioRecordings/{formattedDate}/{fileName}");

                return await UploadDocumentToDropboxAsync(localFilePath, dropBoxPath);
            }
            catch (Exception ex)
            {
                // Log the exception (consider using a logging framework)
                Console.WriteLine($"Error uploading to Dropbox: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UploadDocumentToDropboxAsync(string fileToUpload, string dbxFilePath)
        {
            var dropboxSettings = new DropboxSettings(
                _settings.DropboxAppKey,
                _settings.DropboxAppSecret,
                _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken,
                _settings.DropboxUserEmail
            );

            try
            {
                return await _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath);
            }
            catch (Exception ex)
            {
                // Log the exception (consider using a logging framework)
                Console.WriteLine($"Error in Dropbox upload service: {ex.Message}");
                return false;
            }
        }


        public string blobilpaod(string localFilepath, DateTime recordingDateTime, string contentType)
        {

            try
            {
                string blobUrl = string.Empty;
                var formattedDate = recordingDateTime.ToString("dd_MM_yyyy");
                var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();
                if (azureStorageConnectionString.Count > 0)
                {
                    if (azureStorageConnectionString[0] != null)
                    {
                        string connectionString = azureStorageConnectionString[0];
                        string blobName = Path.GetFileName(localFilepath);
                        string containerName = "audiorecordings";
                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                        containerClient.CreateIfNotExists();
                        /* The container Structure like irfiles/20230925*/
                        BlobClient blobClient = containerClient.GetBlobClient(new string(formattedDate) + "/" + blobName);
                        blobUrl = blobClient.Uri.ToString();
                        using FileStream fs = System.IO.File.OpenRead(localFilepath);
                        var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };
                        /*Commented for local testing ,uncomment when go on live*/
                        blobClient.Upload(fs, new BlobUploadOptions
                        {
                            HttpHeaders = blobHttpHeader
                        });
                        fs.Close();

                    }




                }
                return blobUrl;
            }
            catch(Exception ex)
            {
                return string.Empty;

            }
        }
    }
}
