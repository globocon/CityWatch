using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.Web.Pages.Guard
{
    public class DailyLogPdfModel : PageModel
    {
        public readonly IGuardLogReportGenerator _guardLogReportGenerator;
        public readonly IKeyVehicleLogReportGenerator _keyVehicleLogReportGenerator;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DailyLogPdfModel(IGuardLogReportGenerator guardLogReportGenerator,
            IWebHostEnvironment webHostEnvironment,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator)
        {
            _guardLogReportGenerator = guardLogReportGenerator;
            _webHostEnvironment = webHostEnvironment;
            _keyVehicleLogReportGenerator = keyVehicleLogReportGenerator;
        }

        public IActionResult OnGet(int id, string t, KvlStatusFilter f)
        {
            var pdfFileName = GeneratePdfReport(id, t, f);
            if (!string.IsNullOrEmpty(pdfFileName))
            {
                var pdfFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", pdfFileName);
                if (System.IO.File.Exists(pdfFilePath))
                {
                    var stream = new FileStream(pdfFilePath, FileMode.Open);
                    return new FileStreamResult(stream, "application/pdf");
                }
            }
            return Page();
        }

        public IActionResult OnGetGenerateFilteredDataPdf(int logbookid,string recordids)
        {
            List<int> recordIdsList = recordids?.Split(',').Select(int.Parse)
                                        .ToList() ?? new List<int>();

            var pdfFileName = GenerateFilteredPdfReport(logbookid, recordIdsList);
            if (!string.IsNullOrEmpty(pdfFileName))
            {
                var pdfFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", pdfFileName);
                if (System.IO.File.Exists(pdfFilePath))
                {

                    var stream = new FileStream(pdfFilePath, FileMode.Open);
                    //Response.AppendHeader("Content-Disposition", "inline; filename= " + pdfFileName);
                    Response.ContentType = "application/pdf";
                    Response.Headers["Content-Disposition"] = $"inline; filename={pdfFileName}";
                    return new FileStreamResult(stream, "application/pdf")
                    {
                        FileDownloadName = pdfFileName
                    };
                }
            }
            return NotFound(); // Handle error more appropriately
        }

        private string GenerateFilteredPdfReport(int logbookid, List<int> recordids)
        {
            return _keyVehicleLogReportGenerator.GeneratePdfReportWithIds(logbookid, recordids);
        }

        private string GeneratePdfReport(int id, string type, KvlStatusFilter kvlStatusFilter)
        {
            if (type == "gl")
                return _guardLogReportGenerator.GeneratePdfReport(id);
            else if (type == "vl")
                return _keyVehicleLogReportGenerator.GeneratePdfReport(id, kvlStatusFilter);

            throw new ArgumentException("Invalid type");
        }
    }
}