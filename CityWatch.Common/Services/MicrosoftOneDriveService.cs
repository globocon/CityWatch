using Azure.Core;
using CityWatch.Common.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Core;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using Azure.Identity;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;


namespace CityWatch.Common.Services
{
    public interface IMicrosoftOneDriveService
    {
          Task<bool> Upload( string filename, string dbxFilePath);

        //Task<bool> Download(DropboxSettings settings, string downloadToFolder, string[] filesToDownload);

        //Task<bool> CreateFolder(DropboxSettings settings, string newfolderNameIncludingPath);
    }
    public class MicrosoftOneDriveService: IMicrosoftOneDriveService
    {
      


        private static string clientId = "30ce4d57-3e45-4107-a1f4-57ea39e7ccb5";
        private static string tenantId = "6830b4c5-05d1-4c1f-9a84-40b59bbf6641";
        private static string clientSecret = "-Iw8Q~j54_OJObXsDG81qeqiHd61j9FGOs-BtbY9";


        public async Task<bool> Upload(string filename, string dbxFilePath)
        {
            var tokenCredential = new ClientSecretCredential(
        tenantId,
        clientId,
        clientSecret
    );

            var graphClient = new GraphServiceClient(tokenCredential, new[] { "https://graph.microsoft.com/.default" });

            var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();

            //var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            //{
            //    requestMessage.Headers.Authorization =
            //        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            //    return Task.CompletedTask;
            //}));

            string filePath = filename;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // STEP 1: Create an upload session
                var meDrive = await graphClient.Me.Drive.GetAsync();
                var uploadSession = await graphClient.Drives[meDrive.Id].Root.ItemWithPath(dbxFilePath).CreateUploadSession.PostAsync(new CreateUploadSessionPostRequestBody());
                // STEP 2: Upload the file in chunks
                int maxSliceSize = 320 * 1024; // 320KB per chunk (can be bigger, e.g., 5MB)
                //var uploadProvider = new ChunkedUploadProvider(uploadSession, graphClient, fileStream, maxSliceSize);
                var uploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize, graphClient.RequestAdapter);
                var result = await uploadTask.UploadAsync();

                if (result.UploadSucceeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }

                //var chunkRequests = uploadProvider.GetUploadChunkRequests();
                //var exceptions = new List<Exception>();
                //DriveItem itemResult = null;

                //foreach (var request in chunkRequests)
                //{
                //    var result = await uploadProvider.GetChunkRequestResponseAsync(request, exceptions);

                //    if (result.UploadSucceeded)
                //    {
                //        itemResult = result.ItemResponse;
                //    }
                //}

                //if (itemResult != null)
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
            }

        }


    }
}
