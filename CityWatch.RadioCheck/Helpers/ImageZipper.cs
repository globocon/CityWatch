using DocumentFormat.OpenXml.Presentation;
using System;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Components.Forms;
using System.Drawing.Imaging;

namespace CityWatch.RadioCheck.Helpers
{
    public class ImageZipper
    {
        public static void CreateImageZip(string sourceImageFolder, string destinationFolder, string zipFileName)
        {
            // Validate parameters
            if (!Directory.Exists(sourceImageFolder))
            {
                // throw new DirectoryNotFoundException($"Source image folder not found: {sourceImageFolder}");
                return;
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string zipFilePath = Path.Combine(destinationFolder, zipFileName);

            // Delete existing zip file if exists
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            // Create the zip file
            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    string[] imageFiles = Directory.GetFiles(sourceImageFolder, "*.*")
                        ?? Array.Empty<string>();

                    foreach (var file in imageFiles)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp" || extension == ".tiff")
                        {
                            string fileName = Path.GetFileName(file);
                            archive.CreateEntryFromFile(file, fileName, CompressionLevel.Optimal);
                        }
                    }
                }
            }

            Console.WriteLine($"Zip file created at: {zipFilePath}");
        }

        public static void CreateCompressedImage(string sourceImageFolder, string destinationFolder)
        {

            // Validate parameters
            if (!Directory.Exists(sourceImageFolder))
            {
                // throw new DirectoryNotFoundException($"Source image folder not found: {sourceImageFolder}");
                return;
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            double scaleFactor = 0.5;
            string[] imageFiles = Directory.GetFiles(sourceImageFolder, "*.*")
                        ?? Array.Empty<string>();

            foreach (var file in imageFiles)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp" || extension == ".tiff")
                {
                    string fileName = Path.GetFileName(file);
                    using (var image = Image.FromFile(file))
                    {
                        var newWidth = (int)(image.Width * scaleFactor);
                        var newHeight = (int)(image.Height * scaleFactor);
                        var thumbnailImg = new Bitmap(newWidth, newHeight);
                        var thumbGraph = Graphics.FromImage(thumbnailImg);
                        thumbGraph.CompositingQuality = CompositingQuality.HighQuality;
                        thumbGraph.SmoothingMode = SmoothingMode.HighQuality;
                        thumbGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        var imageRectangle = new Rectangle(0, 0, newWidth, newHeight);
                        thumbGraph.DrawImage(image, imageRectangle);
                        var targetfilewithPath = Path.Combine(destinationFolder, fileName);
                        thumbnailImg.Save(targetfilewithPath, image.RawFormat);
                    }
                }
            }
        }


        public static void CreateThumbnail(string sourceImageFolder, string destinationFolder)
        {
            // Validate parameters
            if (!Directory.Exists(sourceImageFolder))
            {
                // throw new DirectoryNotFoundException($"Source image folder not found: {sourceImageFolder}");
                return;
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            string[] imageFiles = Directory.GetFiles(sourceImageFolder, "*.*")
                        ?? Array.Empty<string>();

            int thumbWidth = 150;
            int thumbHeight = 150;
            foreach (var file in imageFiles)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp" || extension == ".tiff")
                {
                    string fileName = Path.GetFileName(file);
                    var sourcefilewithPath = Path.Combine(sourceImageFolder, fileName);
                    using (Image originalImage = Image.FromFile(sourcefilewithPath))
                    {
                        // Generate thumbnail while preserving aspect ratio
                        Image thumbnail = originalImage.GetThumbnailImage(thumbWidth, thumbHeight, () => false, IntPtr.Zero);

                        // Save the thumbnail as a JPEG (can be PNG, BMP, etc.)
                        var targetfilewithPath = Path.Combine(destinationFolder, fileName);
                        thumbnail.Save(targetfilewithPath, ImageFormat.Jpeg);
                        Console.WriteLine("Thumbnail saved to: " + destinationFolder);
                    }
                }
            }
        }

    }
}
