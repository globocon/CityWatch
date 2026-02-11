// Image Compression Prototype - Created on 11-02-2026
using ImageMagick;
using System;
using System.IO;

namespace CityWatch.Web.Helpers
{
    public static class ImageHelper
    {
        /// <summary>
        /// Compresses an image at the specified path.
        /// </summary>
        /// <param name="filePath">Absolute path to the image file.</param>
        /// <param name="quality">Compression quality (1-100). Default is 50.</param>
        public static void CompressImage(string filePath, int quality = 50)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var extension = Path.GetExtension(filePath).ToLower();
                // Only compress common web image formats
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif" && extension != ".bmp") return;

                using (var image = new MagickImage(filePath))
                {
                    // Resize logic: If image is larger than 800px in either dimension, scale it down.
                    // 800px is very small but still readable for document attachments.
                    int maxDimension = 800;
                    if (image.Width > maxDimension || image.Height > maxDimension)
                    {
                        var size = new MagickGeometry(maxDimension, maxDimension)
                        {
                            IgnoreAspectRatio = false,
                            Greater = true // Only resize if larger than geometry
                        };
                        image.Resize(size);
                    }

                    // Set compression quality
                    image.Quality = quality;

                    // Advanced JPG optimization
                    if (extension == ".jpg" || extension == ".jpeg")
                    {
                        image.Interlace = Interlace.Plane; // Progressive JPG
                        image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
                    }
                    
                    // Strip metadata (EXIF, profiles, etc.) to further reduce file size
                    image.Strip();

                    // Overwrite the original file with the compressed version
                    image.Write(filePath);
                }
            }
            catch (Exception ex)
            {
                // Prototype: Fail gracefully to avoid breaking the upload process
                Console.WriteLine($"Image compression failed for {filePath}: {ex.Message}");
            }
        }
    }
}
