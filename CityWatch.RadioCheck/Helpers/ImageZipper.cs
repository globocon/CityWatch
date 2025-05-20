using System;
using System.IO;
using System.IO.Compression;

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
    }
}
