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

        /// <summary>
        /// The exact differing text for every page in <see cref="TextDifferencePages"/>.
        /// This is what tells you whether a difference is a harmless clock-dependent value
        /// or a real data change.
        /// </summary>
        public List<TextDifference> TextDifferences { get; set; } = new();

        /// <summary>Pages whose embedded-image count differs (a missing chart shows up here).</summary>
        public List<int> ImageDifferencePages { get; set; } = new();

        public List<string> Notes { get; set; } = new();

        /// <summary>First concrete difference, for display.</summary>
        public string FirstDifference { get; set; }
    }

    /// <summary>Pinpointed difference between the same page in the two documents.</summary>
    public class TextDifference
    {
        public int Page { get; set; }

        /// <summary>Character offset into the normalised page text where they diverge.</summary>
        public int CharacterPosition { get; set; }

        /// <summary>What the legacy document has at that point.</summary>
        public string LegacyText { get; set; }

        /// <summary>What the fast document has at that point.</summary>
        public string FastText { get; set; }

        public string ContextBefore { get; set; }
        public string ContextAfter { get; set; }

        public string Summary { get; set; }
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
                foreach (var page in result.TextDifferencePages)
                {
                    result.TextDifferences.Add(
                        DescribeTextDifference(page, legacy[page - 1].Text, fast[page - 1].Text));
                }

                result.FirstDifference = result.TextDifferences[0].Summary;
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

            var masked = MaskedStamps(legacy.Select(z => z.Text));
            if (masked.Count > 0)
            {
                result.Notes.Add(
                    "Excluded from the comparison (render-time values that differ between any two " +
                    $"runs, including two runs of the same generator): {string.Join("; ", masked)}.");
            }

            if (result.IsIdentical && result.LegacyBytes != result.FastBytes)
            {
                result.Notes.Add(
                    $"File sizes differ by {Math.Abs(result.FastBytes - result.LegacyBytes):N0} bytes. " +
                    "Visible content is identical, so this is the render-time stamp above and/or PDF " +
                    "metadata (creation timestamp and document ID).");
            }

            return result;
        }

        /// <summary>
        /// Locates the exact character where two pages diverge and reports the surrounding
        /// context from both sides.
        ///
        /// Printing the start of the page (the previous behaviour) is useless when the pages
        /// share a heading - both previews look identical and the real difference stays
        /// hidden further down. A one-character difference has to be pinpointed to be
        /// diagnosable at all.
        /// </summary>
        private static TextDifference DescribeTextDifference(int page, string legacyText, string fastText)
        {
            const int context = 70;

            var a = Normalise(legacyText);
            var b = Normalise(fastText);

            // First position where they diverge.
            var limit = Math.Min(a.Length, b.Length);
            var start = 0;
            while (start < limit && a[start] == b[start])
                start++;

            if (start == limit && a.Length == b.Length)
            {
                // Normalised text matches - the hash difference came from whitespace only.
                return new TextDifference
                {
                    Page = page,
                    Summary = $"Page {page}: only whitespace differs; visible text is identical."
                };
            }

            // Walk back from the end to bound the changed region.
            var endA = a.Length - 1;
            var endB = b.Length - 1;
            while (endA >= start && endB >= start && a[endA] == b[endB])
            {
                endA--;
                endB--;
            }

            var legacyChanged = start <= endA ? a.Substring(start, endA - start + 1) : "(nothing)";
            var fastChanged = start <= endB ? b.Substring(start, endB - start + 1) : "(nothing)";

            var before = a.Substring(Math.Max(0, start - context), start - Math.Max(0, start - context));
            var afterFrom = Math.Min(a.Length, endA + 1);
            var after = a.Substring(afterFrom, Math.Min(context, a.Length - afterFrom));

            return new TextDifference
            {
                Page = page,
                CharacterPosition = start,
                LegacyText = legacyChanged,
                FastText = fastChanged,
                ContextBefore = before,
                ContextAfter = after,
                Summary =
                    $"Page {page} at character {start}: legacy has \"{Truncate(legacyChanged)}\", " +
                    $"fast has \"{Truncate(fastChanged)}\". Context: ...{Truncate(before, 70)}[HERE]{Truncate(after, 70)}..."
            };
        }

        private static string Truncate(string value, int max = 120)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            return value.Length <= max ? value : value.Substring(0, max) + "...";
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
        /// Values the report stamps from the wall clock at render time. These differ between
        /// ANY two runs, including two runs of the same generator, so comparing them would
        /// only ever measure how far apart the runs started.
        ///
        /// Each pattern is deliberately anchored to its label rather than matching bare
        /// dates or times, so a date that is genuine report *data* is still compared.
        /// </summary>
        private static readonly (string Name, Regex Pattern, string Replacement)[] VolatileStamps =
        {
            // ReportGenerator.cs:609 <- MonthlyKpiResult.cs:21 "{DateTime.Now:dd MMM yyyy @ HH:mm} hrs"
            ("Data Generated timestamp",
             new Regex(@"Data Generated:\s*\d{1,2} \w{3} \d{4} @ \d{1,2}:\d{2} hrs", RegexOptions.Compiled),
             "Data Generated: <RENDER-TIME>"),

            // MonthlySummaryReportGenerator.cs:585 "Release : {DateTime.Now:dddd, dd MMMM yyyy}"
            ("Release date",
             new Regex(@"Release\s*:\s*\w+day, \d{1,2} \w+ \d{4}", RegexOptions.Compiled),
             "Release : <RENDER-DATE>")
        };

        /// <summary>
        /// Collapses whitespace so that insignificant line-wrapping differences in text
        /// extraction do not register as content changes, and masks render-time stamps.
        /// </summary>
        private static string Normalise(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var result = Regex.Replace(text, @"\s+", " ").Trim();
            foreach (var stamp in VolatileStamps)
                result = stamp.Pattern.Replace(result, stamp.Replacement);

            return result;
        }

        /// <summary>
        /// Which render-time stamps were actually masked, so the report says plainly what it
        /// excluded rather than quietly hiding a difference.
        /// </summary>
        private static List<string> MaskedStamps(IEnumerable<string> pageTexts)
        {
            var joined = string.Join(" ", pageTexts);
            return VolatileStamps
                .Where(s => s.Pattern.IsMatch(Regex.Replace(joined ?? string.Empty, @"\s+", " ")))
                .Select(s => s.Name)
                .ToList();
        }

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
