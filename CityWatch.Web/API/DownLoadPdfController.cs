using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System;
using Microsoft.Extensions.Configuration;
using CityWatch.Data;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using System.ComponentModel;


namespace CityWatch.Web.API
{

    [Route("api/[controller]")]
    [ApiController]
    public class DownLoadPdfController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DownLoadPdfController(IConfiguration configuration)
        {

            _configuration = configuration;

        }



        //    [Route("[action]", Name = "PdfDownload")]
        //    public IActionResult PdfDownload(string containerName, string fileName)
        //    {
        //        var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();

        //        try
        //        {


        //                // Create a BlobServiceClient to interact with the Azure Blob Storage account
        //                var blobServiceClient = new BlobServiceClient(azureStorageConnectionString[0]);

        //                // Get a reference to the container and blob
        //                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        //                BlobClient blobClient = containerClient.GetBlobClient(fileName);

        //                // Check if the blob exists
        //                if (!blobClient.Exists())
        //                {
        //                    return NotFound("The PDF file does not exist.");
        //                }

        //                // Generate a SAS token to allow temporary access to the blob
        //                BlobSasBuilder sasBuilder = new BlobSasBuilder
        //                {
        //                    BlobContainerName = containerName,
        //                    BlobName = fileName,
        //                    Resource = "b",
        //                    StartsOn = DateTimeOffset.UtcNow,
        //                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30) // Adjust the expiration time as needed

        //                };
        //                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
        //                // Get the SAS token as a query string
        //                string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential("c4istorage1", "12Q6IOpVChRVlh/gCuYdYoZfkWCDhV82f02Cu9md5jDbiAkxeHuyShvCXrIazkrZYuYj0QmY73ii+AStLR19UQ==")).ToString();

        //                // Construct the URL to open the PDF file in a new tab
        //                string pdfUrl = blobClient.Uri + "?" + sasToken;

        //                // Redirect the user to the PDF URL
        //                return Redirect(pdfUrl);

        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        //        }
        //    }


        //}

        [Route("[action]", Name = "PdfDownload")]
        public ActionResult PdfDownload(string containerName, string fileName)
        {
            var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();

            try
            {
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
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }



    }
}
