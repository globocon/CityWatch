using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Hosting;
using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Net.WebRequestMethods;
using IO = System.IO;
//using Aspose.Slides;
//using Aspose.Slides.Export;
namespace CityWatch.Web.Services
{
    public interface IPptxToHtmlService
    {
        //List<string> ConvertToSlides(string hrreferenceno, string fileName);
        
    }
        public class PptxToHtmlService: IPptxToHtmlService
        {
        private readonly string _reportRootDir;
        private const double EMU_PER_PIXEL = 9525.0; // PowerPoint uses EMUs (English Metric Units)

        public PptxToHtmlService(IWebHostEnvironment webHostEnvironment)
        {
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath);
        }
        //public List<string> ConvertToSlides(string hrreferenceno, string fileName)
        //    {
        //        List<string> slidesHtml = new();
        //    string pptxPath = IO.Path.Combine(_reportRootDir, "TA", hrreferenceno, "Course", fileName);
        //    using (PresentationDocument ppt = PresentationDocument.Open(pptxPath, false))
        //    {
        //        var slides = ppt.PresentationPart.SlideParts.ToList();

        //        int slideIndex = 1;
        //        foreach (var slidePart in slides)
        //        {
        //            StringBuilder sb = new();
        //            sb.AppendLine("<div class='slide' style='position:relative;width:960px;height:540px;background:#fff;overflow:hidden;'>");

        //            // --- TEXT SHAPES ---
        //            foreach (var shape in slidePart.Slide.Descendants<Shape>())
        //            {
        //                var textBody = shape.TextBody;
        //                if (textBody == null) continue;

        //                string text = string.Join(" ", textBody.Descendants<A.Text>().Select(t => t.Text));
        //                if (string.IsNullOrWhiteSpace(text)) continue;

        //                var transform = shape.ShapeProperties?.Transform2D;
        //                if (transform == null) continue;

        //                double left = transform.Offset.X / EMU_PER_PIXEL;
        //                double top = transform.Offset.Y / EMU_PER_PIXEL;
        //                double width = transform.Extents.Cx / EMU_PER_PIXEL;
        //                double height = transform.Extents.Cy / EMU_PER_PIXEL;

        //                sb.AppendLine(
        //                    $"<div style='position:absolute;left:{left}px;top:{top}px;width:{width}px;height:{height}px;font-size:18px;color:#111;'>{System.Net.WebUtility.HtmlEncode(text)}</div>");
        //            }

        //            // --- IMAGES (Base64 embedded) ---
        //            foreach (var picture in slidePart.Slide.Descendants<Aspose.Slides.Picture>())
        //            {
        //                var blip = picture.BlipFill?.Blip;
        //                if (blip == null) continue;

        //                var imagePart = (ImagePart)slidePart.GetPartById(blip.Embed.Value);

        //                using var ms = new MemoryStream();
        //                imagePart.GetStream().CopyTo(ms);
        //                string base64 = Convert.ToBase64String(ms.ToArray());
        //                string mimeType = imagePart.ContentType;

        //                var transform = picture.ShapeProperties?.Transform2D;
        //                if (transform == null) continue;

        //                double left = transform.Offset.X / EMU_PER_PIXEL;
        //                double top = transform.Offset.Y / EMU_PER_PIXEL;
        //                double width = transform.Extents.Cx / EMU_PER_PIXEL;
        //                double height = transform.Extents.Cy / EMU_PER_PIXEL;

        //                sb.AppendLine(
        //                    $"<img src='data:{mimeType};base64,{base64}' " +
        //                    $"style='position:absolute;left:{left}px;top:{top}px;width:{width}px;height:{height}px;' />");
        //            }

        //            sb.AppendLine("</div>");
        //            slidesHtml.Add(sb.ToString());
        //            slideIndex++;
        //        }
        //    }
        //    return slidesHtml;
        //    }
        //private static string GetImageExtension(string contentType) => contentType switch
        //{
        //    "image/jpeg" => ".jpg",
        //    "image/png" => ".png",
        //    "image/gif" => ".gif",
        //    _ => ".img"
        //};
    }
}
