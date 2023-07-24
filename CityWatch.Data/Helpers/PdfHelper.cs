using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using System.Collections.Generic;

namespace CityWatch.Data.Helpers
{
    public static class PdfHelper
    {
        public static PdfFont GetPdfFont()
        {
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
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
    }
}
