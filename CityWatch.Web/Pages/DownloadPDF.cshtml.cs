using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace CityWatch.Web.Pages
{
    public class DownloadPDFModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DownloadPDFModel(IConfiguration configuration)
        {

            _configuration = configuration;

        }

        public IActionResult OnGet()
        {
            string containerName = Request.Query["containerName"];
            string fileName = Request.Query["fileName"];
            var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();
            // Create a BlobServiceClient to interact with the Azure Blob Storage account
            var blobServiceClient = new BlobServiceClient(azureStorageConnectionString[0]);
            // Get a reference to the container and blob
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            MemoryStream memoryStream = new MemoryStream();
            blobClient.DownloadTo(memoryStream);
            memoryStream.Position = 0;
            return File(memoryStream, "application/pdf", fileName);
        }
    }
}
