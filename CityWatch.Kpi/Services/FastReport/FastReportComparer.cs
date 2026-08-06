using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CityWatch.Kpi.Services.FastReport
{
    public class PdfComparisonResult
    {
        public bool IsIdentical { get; set; }

        public int LegacyPageCount { get; set; }
        public int FastPageCount { get; set; }

        public long LegacyBytes { get; set; }
        public long FastBytes { get; set; }

        /// <summary>Pages whose extracted text differs, 1-based.</summary>
        public List<int> TextDifferencePages { get; set; } = new();

        /// <summary>Pages whose embedded-image count differs (a missing chart shows up here).</summary>
        public List<int> ImageDifferencePages { get; set; } = new();

        public List<string> Notes { get; set; } = new();

        /// <summary>First concrete difference, for display.</summary>
        public string FirstDifference { get; set; }
    }

    /// <summary>
    /// Structural comparison of two generated PDFs.
    ///
    /// Raw byte equality is not a usable test: iText stamps every document with a creation
    /// timestamp, a modification timestamp and a random document ID, so two runs of the
    /// *same* generator one second apart already differ at the byte level. What must match
    /// is the content, so this compares:
    ///
    ///   1. page count
    ///   2. normalised extracted text, page by page
    ///   3. the number of embedded images per page (charts are rasterised images, so a
    ///      chart that silently failed to render shows up as a count mismatch)
    ///
    /// File size is reported for information but is not part of the verdict.
    /// </summary>
    public static class FastReportComparer
    {
        public static PdfComparisonResult Compare(byte[] legacyPdf, byte[] fastPdf)
        {
            var result = new PdfComparisonResult
            {
                LegacyBytes = legacyPdf?.LongLength ?? 0,
                FastBytes = fastPdf?.LongLength ?? 0
            };

            if (legacyPdf == null || legacyPdf.Length == 0)
            {
                result.FirstDifference = "The legacy generator produced no output.";
                return result;
            }

            if (fastPdf == null || fastPdf.Length == 0)
            {
                result.FirstDifference = "The fast generator produced no output.";
                return result;
            }

            var legacy = Read(legacyPdf);
            var fast = Read(fastPdf);

            result.LegacyPageCount = legacy.Count;
            result.FastPageCount = fast.Count;

            if (legacy.Count != fast.Count)
            {
                result.FirstDifference = $"Page count differs: legacy {legacy.Count}, fast {fast.Count}.";
                return result;
            }

            for (var i = 0; i < legacy.Count; i++)
            {
                if (!string.Equals(legacy[i].TextHash, fast[i].TextHash, StringComparison.Ordinal))
                    result.TextDifferencePages.Add(i + 1);

                if (legacy[i].ImageCount != fast[i].ImageCount)
                    result.ImageDifferencePages.Add(i + 1);
            }

            if (result.TextDifferencePages.Count > 0)
            {
                var page = result.TextDifferencePages[0];
                result.FirstDifference =
                    $"Text differs on page {page}. " +
                    $"Legacy starts '{Preview(legacy[page - 1].Text)}', fast starts '{Preview(fast[page - 1].Text)}'.";
            }
            else if (result.ImageDifferencePages.Count > 0)
            {
                var page = result.ImageDifferencePages[0];
                result.FirstDifference =
                    $"Embedded image count differs on page {page}: " +
                    $"legacy {legacy[page - 1].ImageCount}, fast {fast[page - 1].ImageCount}. " +
                    "This usually means a chart failed to render in one of the runs.";
            }

            result.IsIdentical = result.TextDifferencePages.Count == 0 && result.ImageDifferencePages.Count == 0;

            if (result.IsIdentical && result.LegacyBytes != result.FastBytes)
            {
                result.Notes.Add(
                    $"File sizes differ by {Math.Abs(result.FastBytes - result.LegacyBytes):N0} bytes. " +
                    "Content is identical, so this is PDF metadata (creation timestamp and document ID), " +
                    "which differs between any two runs including two runs of the same generator.");
            }

            return result;
        }

        private static string Preview(string text)
        {
            if (string.IsNullOrEmpty(text)) return "(empty)";
            var trimmed = text.Trim();
            return trimmed.Length <= 60 ? trimmed : trimmed.Substring(0, 60) + "...";
        }

        private static List<PageFingerprint> Read(byte[] pdfBytes)
        {
            var pages = new List<PageFingerprint>();

            using var reader = new PdfReader(new System.IO.MemoryStream(pdfBytes));
            using var document = new PdfDocument(reader);

            for (var pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
            {
                var page = document.GetPage(pageNumber);
                var text = PdfTextExtractor.GetTextFromPage(page);
                var normalised = Normalise(text);

                pages.Add(new PageFingerprint
                {
                    Text = text,
                    TextHash = Hash(normalised),
                    ImageCount = CountImages(page)
                });
            }

            return pages;
        }

        /// <summary>
        /// Collapses whitespace so that insignificant line-wrapping differences in text
        /// extraction do not register as content changes.
        /// </summary>
        private static string Normalise(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : Regex.Replace(text, @"\s+", " ").Trim();

        private static string Hash(string value)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static int CountImages(PdfPage page)
        {
            try
            {
                var resources = page.GetResources();
                var xObjects = resources?.GetResource(PdfName.XObject);
                if (xObjects == null)
                    return 0;

                return xObjects.KeySet()
                    .Select(key => xObjects.GetAsStream(key))
                    .Count(stream => stream != null
                                     && PdfName.Image.Equals(stream.GetAsName(PdfName.Subtype)));
            }
            catch
            {
                return -1;
            }
        }

        private sealed class PageFingerprint
        {
            public string Text { get; init; }
            public string TextHash { get; init; }
            public int ImageCount { get; init; }
        }
    }
}
