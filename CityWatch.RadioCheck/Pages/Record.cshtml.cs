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

        public async Task<IActionResult> OnPostSaveAudioAsync(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No file received or file is empty.");
            }

            // Generate a unique file name
            var uniqueFileName = GenerateUniqueFileName(audioFile.FileName);

            // Define the folder path for audio recording
            var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", DateTime.Today.ToString("dd_MM_yyyy"));

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            // Save the file to the server
            var filePath = Path.Combine(folderPath, uniqueFileName);
            await SaveFileAsync(audioFile, filePath);

            
            // Upload the file to Dropbox and Azure bob
            var blobUrl= blobilpaod(filePath);
          

            var clientSite = _clientDataProvider.GetClientSiteForRcLogBook();
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSite.FirstOrDefault().Id);
            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;

            var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{DateTime.Now.Year}/{DateTime.Now.Date:yyyyMM} - {DateTime.Now.Date.ToString("MMMM").ToUpper()} DATA/{DateTime.Now.Date.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(filePath);


            var isUploaded = await UploadFileToDropboxAsync(filePath, dbxFilePath);
            if (isUploaded)
            {
                _guardDataProvider.SaveRecordingFileDetails(new AudioRecordingLog { BlobUrl = blobUrl, FileName = uniqueFileName, DropboxPath= dbxFilePath });
            }
            // Return the result with the unique file name
            return new JsonResult(new { success = isUploaded, fileName = uniqueFileName });
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var ticks = DateTime.UtcNow.Ticks;
            var fileExtension = Path.GetExtension(originalFileName);
            var formattedDate = DateTime.Today.ToString("dd_MM_yyyy");
            return $"recording_{formattedDate}_{ticks}{fileExtension}";
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


        public string blobilpaod(string localFilepath)
        {

            try
            {
                string blobUrl = string.Empty;
                var formattedDate = DateTime.Today.ToString("dd_MM_yyyy");
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
                        var blobHttpHeader = new BlobHttpHeaders { ContentType = "audio/wav" };
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
