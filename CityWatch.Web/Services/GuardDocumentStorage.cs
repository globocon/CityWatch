using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// "GuardDocumentStorage" section of appsettings.json.
    /// </summary>
    public class GuardDocumentStorageOptions
    {
        public const string Name = "GuardDocumentStorage";

        /// <summary>
        /// Off (the default) = files live only on the VM disk, exactly as before.
        /// </summary>
        public bool UseBlobStorage { get; set; }

        public string ContainerName { get; set; } = "guard-docs";

        /// <summary>
        /// While true, every file is written to the VM disk as well as to Blob (safe first rollout).
        /// Set false once Blob is proven - new files then go to Blob only.
        /// </summary>
        public bool KeepLocalCopy { get; set; } = true;

        /// <summary>
        /// How long a download link handed to the browser / mobile app stays valid.
        /// </summary>
        public int LinkExpiryMinutes { get; set; } = 15;

        /// <summary>
        /// Optional. When empty, AzureStorage:AzureStorageConnectionString is used.
        /// </summary>
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// A file that can be read from local disk. When the document only exists in Blob it is
    /// downloaded to a temp file, which is deleted again on Dispose.
    /// </summary>
    public sealed class GuardDocumentFile : IDisposable
    {
        private readonly bool _isTemporary;

        public GuardDocumentFile(string path, bool isTemporary)
        {
            Path = path;
            _isTemporary = isTemporary;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (!_isTemporary)
                return;

            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch
            {
                // a leftover temp file is harmless
            }
        }
    }

    /// <summary>
    /// Storage for guard HR documents (licences, compliance docs, training certificates and the
    /// practical / RPL evidence files). Paths are relative to wwwroot/Uploads/Guards/License, e.g.
    /// "{SecurityNo}/{FileName}" or "{SecurityNo}/CertificateDocuments/{Course}/{FileName}".
    /// The blob name is the same path prefixed with "License/", which is what
    /// "azcopy copy wwwroot/Uploads/Guards/License https://.../guard-docs --recursive" produces.
    /// </summary>
    public interface IGuardDocumentStorage
    {
        bool IsBlobEnabled { get; }

        string GetLocalPath(string relativePath);

        /// <summary>
        /// Saves (or replaces) a document.
        /// </summary>
        void Save(string relativePath, Stream content);

        Task SaveAsync(string relativePath, Stream content);

        /// <summary>
        /// Copies a document that was just written to its local path into Blob, removing the
        /// local copy when KeepLocalCopy is off.
        /// </summary>
        void PublishLocalFile(string relativePath);

        /// <summary>
        /// Gives a readable local file for code that needs a file path (Dropbox upload, PDF merge).
        /// If the document is nowhere to be found, the (missing) local path is returned so callers
        /// fail exactly as they did before.
        /// </summary>
        GuardDocumentFile OpenLocalCopy(string relativePath);

        /// <summary>
        /// Short-lived read link for a document held in Blob, or null if it is not in Blob.
        /// </summary>
        Task<Uri> GetBlobReadUriAsync(string relativePath);

        /// <summary>
        /// Streams a document held in Blob. Used when a read link cannot be generated.
        /// Returns false if it is not in Blob.
        /// </summary>
        Task<bool> TryCopyFromBlobAsync(string relativePath, Stream destination);
    }

    public class GuardDocumentStorage : IGuardDocumentStorage
    {
        public const string BlobPrefix = "License/";

        private readonly GuardDocumentStorageOptions _options;
        private readonly string _localRoot;
        private readonly ILogger<GuardDocumentStorage> _logger;
        private readonly BlobContainerClient _container;
        private readonly FileExtensionContentTypeProvider _contentTypes = new FileExtensionContentTypeProvider();
        private volatile bool _containerChecked;

        public GuardDocumentStorage(IOptions<GuardDocumentStorageOptions> options,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<GuardDocumentStorage> logger)
        {
            _options = options.Value ?? new GuardDocumentStorageOptions();
            _localRoot = Path.Combine(webHostEnvironment.WebRootPath, "Uploads", "Guards", "License");
            _logger = logger;

            if (_options.UseBlobStorage)
            {
                var connectionString = string.IsNullOrWhiteSpace(_options.ConnectionString)
                    ? configuration.GetSection("AzureStorage")["AzureStorageConnectionString"]
                    : _options.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError("GuardDocumentStorage: UseBlobStorage is on but no Azure connection string is configured. Falling back to local disk.");
                }
                else
                {
                    _container = new BlobContainerClient(connectionString, _options.ContainerName);
                }
            }
        }

        public bool IsBlobEnabled => _container != null;

        /// <summary>
        /// Joins path parts into a storage path, e.g. RelativePath(securityNo, "CertificateDocuments", course, fileName).
        /// </summary>
        public static string RelativePath(params string[] parts)
        {
            return string.Join("/", parts);
        }

        public string GetLocalPath(string relativePath)
        {
            var parts = new[] { _localRoot }.Concat(SplitPath(relativePath)).ToArray();
            return Path.Combine(parts);
        }

        public void Save(string relativePath, Stream content)
        {
            WriteLocal(relativePath, content);
            PublishLocalFile(relativePath);
        }

        public async Task SaveAsync(string relativePath, Stream content)
        {
            var localPath = PrepareLocal(relativePath);
            using (var stream = new FileStream(localPath, FileMode.Create))
            {
                await content.CopyToAsync(stream);
            }

            if (!IsBlobEnabled)
                return;

            try
            {
                EnsureContainer();
                using (var source = File.OpenRead(localPath))
                {
                    await GetBlob(relativePath).UploadAsync(source, UploadOptions(relativePath));
                }
                RemoveLocalIfNotKept(localPath);
            }
            catch (Exception ex)
            {
                // the local copy is still there and is served from disk, so nothing is lost
                _logger.LogError(ex, "GuardDocumentStorage: upload to Blob failed for {Path}; kept on local disk.", relativePath);
            }
        }

        public void PublishLocalFile(string relativePath)
        {
            if (!IsBlobEnabled)
                return;

            var localPath = GetLocalPath(relativePath);
            if (!File.Exists(localPath))
                return;

            try
            {
                EnsureContainer();
                using (var source = File.OpenRead(localPath))
                {
                    GetBlob(relativePath).Upload(source, UploadOptions(relativePath));
                }
                RemoveLocalIfNotKept(localPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GuardDocumentStorage: upload to Blob failed for {Path}; kept on local disk.", relativePath);
            }
        }

        public GuardDocumentFile OpenLocalCopy(string relativePath)
        {
            var localPath = GetLocalPath(relativePath);
            if (File.Exists(localPath) || !IsBlobEnabled)
                return new GuardDocumentFile(localPath, false);

            try
            {
                var blob = GetBlob(relativePath);
                if (!blob.Exists())
                    return new GuardDocumentFile(localPath, false);

                var tempDir = Path.Combine(Path.GetTempPath(), "CityWatchGuardDocs");
                Directory.CreateDirectory(tempDir);
                // same file name as the original - the Dropbox upload and image checks go by name/extension
                var tempPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempPath);
                tempPath = Path.Combine(tempPath, Path.GetFileName(localPath));

                blob.DownloadTo(tempPath);
                return new GuardDocumentFile(tempPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GuardDocumentStorage: download from Blob failed for {Path}.", relativePath);
                return new GuardDocumentFile(localPath, false);
            }
        }

        public async Task<Uri> GetBlobReadUriAsync(string relativePath)
        {
            if (!IsBlobEnabled)
                return null;

            var blob = GetBlob(relativePath);
            if (!blob.CanGenerateSasUri || !(await blob.ExistsAsync()).Value)
                return null;

            return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.LinkExpiryMinutes)));
        }

        public async Task<bool> TryCopyFromBlobAsync(string relativePath, Stream destination)
        {
            if (!IsBlobEnabled)
                return false;

            var blob = GetBlob(relativePath);
            if (!(await blob.ExistsAsync()).Value)
                return false;

            await blob.DownloadToAsync(destination);
            return true;
        }

        private void WriteLocal(string relativePath, Stream content)
        {
            var localPath = PrepareLocal(relativePath);
            using var stream = File.Create(localPath);
            content.CopyTo(stream);
        }

        /// <summary>
        /// Creates the folder and removes any existing file of the same name - the behaviour the
        /// upload handlers had before.
        /// </summary>
        private string PrepareLocal(string relativePath)
        {
            var localPath = GetLocalPath(relativePath);
            var dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(localPath))
                File.Delete(localPath);

            return localPath;
        }

        private void RemoveLocalIfNotKept(string localPath)
        {
            if (_options.KeepLocalCopy)
                return;

            try
            {
                File.Delete(localPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GuardDocumentStorage: could not remove local copy {Path} after upload.", localPath);
            }
        }

        private void EnsureContainer()
        {
            if (_containerChecked)
                return;

            _container.CreateIfNotExists(PublicAccessType.None);
            _containerChecked = true;
        }

        private BlobClient GetBlob(string relativePath)
        {
            return _container.GetBlobClient(BlobPrefix + string.Join("/", SplitPath(relativePath)));
        }

        private BlobUploadOptions UploadOptions(string relativePath)
        {
            if (!_contentTypes.TryGetContentType(relativePath, out var contentType))
                contentType = "application/octet-stream";

            return new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        }

        /// <summary>
        /// Both separators are accepted: on Windows a "\" inside a course name already made a
        /// sub folder on disk, and AzCopy turns that into "/" as well.
        /// </summary>
        private static string[] SplitPath(string relativePath)
        {
            var parts = (relativePath ?? string.Empty)
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Any(p => p == ".." || p == "."))
                throw new ArgumentException($"Invalid guard document path '{relativePath}'.", nameof(relativePath));

            return parts;
        }
    }
}
