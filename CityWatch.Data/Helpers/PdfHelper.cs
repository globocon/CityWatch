using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;

namespace CityWatch.Data.Helpers
{
    public static class PdfHelper
    {
        public static PdfFont GetPdfFont()
        {
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        }

        public static PdfFont GetPdfFont(string font)
        {
            return PdfFontFactory.CreateFont(font);
        }

        public static void CombinePdfReports(string combinedFileName, List<string> fileNames, string summaryFileName)
        {
            var pdfDoc = new PdfDocument(new PdfWriter(combinedFileName));
            var reportDoc = new PdfDocument(new PdfReader(summaryFileName));
            reportDoc.CopyPagesTo(1, reportDoc.GetNumberOfPages(), pdfDoc);
            reportDoc.Close();
            foreach (var fileName in fileNames)
            {
                reportDoc = new PdfDocument(new PdfReader(fileName));
                reportDoc.CopyPagesTo(1, reportDoc.GetNumberOfPages(), pdfDoc);
                reportDoc.Close();
            }
            pdfDoc.Close();
        }
        public static void CombinePdfReportsTimesheet(string combinedFileName, List<string> fileNames, string summaryFileName)
        {
            var pdfDoc = new PdfDocument(new PdfWriter(combinedFileName));

            // Log what's being combined
            Console.WriteLine($"Combining reports into: {combinedFileName}");

            // Add the summary report first (without guard details)
            if (!string.IsNullOrEmpty(summaryFileName))
            {
                Console.WriteLine($"Adding summary report: {summaryFileName}");
                var summaryDoc = new PdfDocument(new PdfReader(summaryFileName));
                summaryDoc.CopyPagesTo(1, summaryDoc.GetNumberOfPages(), pdfDoc);
                summaryDoc.Close();
            }


            pdfDoc.Close();
        }
    }
}
