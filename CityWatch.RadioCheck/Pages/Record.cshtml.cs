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
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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

            string pcmFile1 = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", "recording_1.wav");
            string pcmFile2 = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", "recording_2.wav");
            string outputFilePath = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", "mergedAudio.wav");

            MergePCMFiles(pcmFile1, pcmFile2, outputFilePath);


            string pcmFilePath = pcmFile1;
            string wavFilePath = outputFilePath;

            // Define WAV format parameters
            int sampleRate = 35100; // Sample rate in Hz
            int bitsPerSample = 64; // Bits per sample (e.g., 16-bit PCM)
            int channels = 1; // Number of audio channels (e.g., 2 for stereo)

            //CreateWavFileFromPcm(pcmFilePath, wavFilePath, sampleRate, bitsPerSample, channels);

            ////Save the file to the server
            //var filePath = Path.Combine(folderPath, uniqueFileName);
            //await SaveFileAsync(audioFile, filePath);


            //// Upload the file to Dropbox and Azure bob
            //var blobUrl = blobilpaod(filePath);


            //var clientSite = _clientDataProvider.GetClientSiteForRcLogBook();
            //var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSite.FirstOrDefault().Id);
            //var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;

            //var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            //var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{DateTime.Now.Year}/{DateTime.Now.Date:yyyyMM} - {DateTime.Now.Date.ToString("MMMM").ToUpper()} DATA/{DateTime.Now.Date.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(filePath);


            //var isUploaded = await UploadFileToDropboxAsync(filePath, dbxFilePath);
            //if (isUploaded)
            //{
            //    _guardDataProvider.SaveRecordingFileDetails(new AudioRecordingLog { BlobUrl = blobUrl, FileName = uniqueFileName, DropboxPath = dbxFilePath });
            //}
            // Return the result with the unique file name
            return new JsonResult(new { success = true, fileName = uniqueFileName });
        }


        //public static void MergePCMFiles(string file1Path, string file2Path, string outputFilePath)
        //{
        //    try
        //    {
        //        // Read both PCM files as byte arrays
        //        byte[] pcmData1 = System.IO.File.ReadAllBytes(file1Path);
        //        byte[] pcmData2 = System.IO.File.ReadAllBytes(file2Path);

        //        // Concatenate the two byte arrays
        //        byte[] mergedPCMData = new byte[pcmData1.Length + pcmData2.Length];
        //        Buffer.BlockCopy(pcmData1, 0, mergedPCMData, 0, pcmData1.Length);
        //        Buffer.BlockCopy(pcmData2, 0, mergedPCMData, pcmData1.Length, pcmData2.Length);

        //        // Write the merged PCM data to a new file
        //        System.IO.File.WriteAllBytes(outputFilePath, mergedPCMData);

        //        Console.WriteLine($"PCM files merged successfully. Merged file saved at: {outputFilePath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error merging PCM files: {ex.Message}");
        //    }
        //}



        public static void MergePCMFiles(string file1Path, string file2Path, string outputFilePath)
        {
            try
            {

                using (var reader1 = new WaveFileReader(file1Path))
                using (var reader2 = new WaveFileReader(file2Path))
                {
                    // Ensure both WAV files have the same format
                    if (!reader1.WaveFormat.Equals(reader2.WaveFormat))
                    {
                        throw new InvalidOperationException("WAV files have different formats.");
                    }

                    var waveFormat = reader1.WaveFormat;

                    using (var writer = new WaveFileWriter(outputFilePath, waveFormat))
                    {
                        // Copy data from the first WAV file
                        CopyWaveStream(reader1, writer);

                        // Copy data from the second WAV file
                        CopyWaveStream(reader2, writer);

                        Console.WriteLine($"WAV files concatenated successfully. Output file saved at: {outputFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error merging PCM files: {ex.Message}");
            }
        }

        //working
        //public static void CreateWavFileFromPcm(string pcmFilePath, string wavFilePath, int sampleRate, int bitsPerSample, int channels)
        //{
        //    try
        //    {
        //        // Read the PCM data from the file
        //        byte[] pcmData = System.IO.File.ReadAllBytes(pcmFilePath);

        //        // Define the WAV format
        //        var waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);

        //        // Create and save the WAV file
        //        using (var waveFileWriter = new WaveFileWriter(wavFilePath, waveFormat))
        //        {
        //            // Write the PCM data to the WAV file
        //            waveFileWriter.Write(pcmData, 0, pcmData.Length);

        //            Console.WriteLine($"PCM file converted to WAV successfully. WAV file saved at: {wavFilePath}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error creating WAV file from PCM: {ex.Message}");
        //    }
        //}


        public static void CreateWavFileFromPcm(string pcmFilePath, string wavFilePath, int sampleRate, int bitsPerSample, int channels)
        {
            try
            {
                // Read PCM data from the file
                byte[] pcmData = System.IO.File.ReadAllBytes(pcmFilePath);

                // Create a WAV header and append PCM data
                using (var waveFileWriter = new FileStream(wavFilePath, FileMode.Create))
                {
                    using (var writer = new BinaryWriter(waveFileWriter))
                    {
                        // Calculate sizes
                        int dataChunkSize = pcmData.Length;
                        int fileSize = 36 + dataChunkSize;

                        // Write the WAV header
                        writer.Write(new[] { 'R', 'I', 'F', 'F' }); // ChunkID
                        writer.Write(fileSize); // ChunkSize
                        writer.Write(new[] { 'W', 'A', 'V', 'E' }); // Format

                        writer.Write(new[] { 'f', 'm', 't', ' ' }); // Subchunk1ID
                        writer.Write(16); // Subchunk1Size (16 for PCM)
                        writer.Write((short)1); // AudioFormat (1 for PCM)
                        writer.Write((short)channels); // NumChannels
                        writer.Write(sampleRate); // SampleRate
                        writer.Write(sampleRate * channels * bitsPerSample / 8); // ByteRate
                        writer.Write((short)(channels * bitsPerSample / 8)); // BlockAlign
                        writer.Write((short)bitsPerSample); // BitsPerSample

                        writer.Write(new[] { 'd', 'a', 't', 'a' }); // Subchunk2ID
                        writer.Write(dataChunkSize); // Subchunk2Size

                        // Write PCM data
                        writer.Write(pcmData);
                    }
                }

                Console.WriteLine($"PCM file converted to WAV successfully. WAV file saved at: {wavFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating WAV file from PCM: {ex.Message}");
            }
        }

        private static void CopyWaveStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
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

        private async Task<bool> UploadFileToDropboxAsync(string localFilePath, string dropBoxPath)
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
            catch (Exception ex)
            {
                return string.Empty;

            }
        }
    }
}
