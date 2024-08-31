using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using CityWatch.Data.Providers;
using CityWatch.Data.Models;

namespace CityWatch.RadioCheck.Pages
{
    // Disable Anti-Forgery validation only for this method
    [IgnoreAntiforgeryToken]
    public class LprWebhookModel : PageModel
    {

        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public LprWebhookModel(IGuardLogDataProvider guardLogDataProvider)
        {

            _guardLogDataProvider = guardLogDataProvider;

        }
        public void OnGet()
        {

        }


        // Define a class to match the structure of the incoming JSON data
        public class WebhookRequest
        {
            // Maps to "org_id"
            public string org_id { get; set; }

            // Maps to "webhook_type"
            public string webhook_type { get; set; }

            // Maps to "created_at" as Unix timestamp
            public int created_at { get; set; }

            // Maps to "webhook_id"
            public string webhook_id { get; set; }

            // Maps to "data"
            public WebhookData data { get; set; }
        }

        // Nested data model
        public class WebhookData
        {
            // Maps to "camera_id"
            public string camera_id { get; set; }

            // Maps to "created" as Unix timestamp
            public int created { get; set; }

            // Maps to "license_plate_number"
            public string license_plate_number { get; set; }

            // Maps to "confidence"
            public double confidence { get; set; }
        }

        [BindProperty]
        public WebhookRequest WebhookDataTemp { get; set; }




        private static readonly string sharedSecret = "dileep";  // Replace with your actual shared secret
        
        /// <summary>
        /// Webhook implementaion for LPR
        /// </summary>
        /// <returns>DB save values </returns>
        public async Task<IActionResult> OnPostAsync()
        {

            // Extract the signature and timestamp from headers
            if (!Request.Headers.TryGetValue("Verkada-Signature", out var signatureHeader))
            {
                // Return a 400 Bad Request response if the signature is missing
                return StatusCode(400, new { message = "Missing signature" });
            }

            var signatureParts = signatureHeader.ToString().Split('|');
            if (signatureParts.Length != 2)
            {
                // Return a 400 Bad Request response if the signature format is invalid
                return StatusCode(400, new { message = "Invalid signature format" });
            }

            var timestamp = signatureParts[0];
            var signature = signatureParts[1];

            //Check if the request was sent in the last minute
            if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(timestamp)) > 60)
            {
                // Return a 403 Forbidden response if the request has expired
                return StatusCode(403, new { message = "Expired" });
            }

            // Read the request body
            string requestBody;
            using (var reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            // Create the string to hash
            //var toHash = $"{requestBody}|{timestamp}";

            ////Compute the HMAC SHA256 signature
            //using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret)))
            //{
            //    var computedSignature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(toHash))).Replace("-", "").ToLower();

            //    // Verify the signature
            //    if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(signature), Encoding.UTF8.GetBytes(computedSignature)))
            //    {
            //        // Return a 403 Forbidden response if the signature is invalid
            //        return StatusCode(403, new { message = "Invalid signature" });
            //    }
            //}

            // Deserialize the JSON body to the LprData class
            WebhookDataTemp = System.Text.Json.JsonSerializer.Deserialize<WebhookRequest>(requestBody);


            // Process the data (e.g., log it, store it, or take some action)
            if (WebhookDataTemp != null)
            {
                // Example processing: Print to console (or handle as needed)
                System.Diagnostics.Debug.WriteLine($"License Plate Detected: {WebhookDataTemp.data.license_plate_number} at {WebhookDataTemp.data.created} ");

                _guardLogDataProvider.SaveLprWebhookResponse(
                    new LprWebhookResponse
                    {
                        license_plate_number = WebhookDataTemp.data.license_plate_number,
                        created = WebhookDataTemp.data.created.ToString(),
                        camera_id = WebhookDataTemp.data.camera_id,
                        webhook_id = WebhookDataTemp.webhook_id,
                        CrDateTime = DateTime.Now,
                        ReadStatus = 0

                    });
                // Respond to the LPR system with a success message
                return new JsonResult(new { status = "success", message = "Webhook received successfully" }) { StatusCode = 200 };
            }

            // Return a 400 Bad Request response if the data is null
            return StatusCode(400, new { message = "Invalid data" });



        }


    }



}

