using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// HR document storage with Blob switched off - the default, and what every existing install
    /// runs until the production switch-over. Files must land exactly where they always have.
    /// </summary>
    [TestClass]
    public class GuardDocumentStorageTests
    {
        private string _webRoot;

        [TestInitialize]
        public void Setup()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "GuardDocumentStorageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_webRoot);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_webRoot))
                Directory.Delete(_webRoot, true);
        }

        private GuardDocumentStorage CreateStorage(GuardDocumentStorageOptions options = null, IConfiguration configuration = null)
        {
            return new GuardDocumentStorage(
                Options.Create(options ?? new GuardDocumentStorageOptions()),
                new TestWebHostEnvironment(_webRoot),
                configuration ?? new ConfigurationBuilder().Build(),
                NullLogger<GuardDocumentStorage>.Instance);
        }

        private string LicenseDir(params string[] parts)
        {
            var all = new string[parts.Length + 4];
            all[0] = _webRoot; all[1] = "Uploads"; all[2] = "Guards"; all[3] = "License";
            Array.Copy(parts, 0, all, 4, parts.Length);
            return Path.Combine(all);
        }

        [TestMethod]
        public void Save_BlobOff_WritesToTheSameFolderAsBefore()
        {
            var storage = CreateStorage();

            storage.Save(GuardDocumentStorage.RelativePath("569-829", "CertificateDocuments", "First Aid", "evidence.pdf"),
                new MemoryStream(Encoding.UTF8.GetBytes("v1")));

            var expected = LicenseDir("569-829", "CertificateDocuments", "First Aid", "evidence.pdf");
            Assert.IsFalse(storage.IsBlobEnabled);
            Assert.AreEqual("v1", File.ReadAllText(expected));
        }

        [TestMethod]
        public async Task Save_ReplacesAnExistingFile()
        {
            var storage = CreateStorage();
            var path = GuardDocumentStorage.RelativePath("569-829", "03e_Licence.pdf");

            storage.Save(path, new MemoryStream(Encoding.UTF8.GetBytes("a much longer first version")));
            await storage.SaveAsync(path, new MemoryStream(Encoding.UTF8.GetBytes("v2")));

            Assert.AreEqual("v2", File.ReadAllText(LicenseDir("569-829", "03e_Licence.pdf")));
        }

        [TestMethod]
        public void OpenLocalCopy_BlobOff_ReturnsTheLocalPathEvenWhenMissing()
        {
            var storage = CreateStorage();

            using var file = storage.OpenLocalCopy(GuardDocumentStorage.RelativePath("569-829", "missing.pdf"));

            // callers (Dropbox upload, PDF merge) then fail the way they always did
            Assert.AreEqual(LicenseDir("569-829", "missing.pdf"), file.Path);
        }

        [TestMethod]
        public void OpenLocalCopy_DisposeNeverDeletesTheRealDocument()
        {
            var storage = CreateStorage();
            var path = GuardDocumentStorage.RelativePath("569-829", "keep.pdf");
            storage.Save(path, new MemoryStream(Encoding.UTF8.GetBytes("x")));

            using (storage.OpenLocalCopy(path)) { }

            Assert.IsTrue(File.Exists(LicenseDir("569-829", "keep.pdf")));
        }

        [TestMethod]
        public void SlashInACourseName_MapsToTheSameSubFolderAsPathCombineDid()
        {
            var storage = CreateStorage();

            var local = storage.GetLocalPath(GuardDocumentStorage.RelativePath("569-829", "RPLCertificateDocuments", "Level 3/LB", "a.pdf"));

            Assert.AreEqual(LicenseDir("569-829", "RPLCertificateDocuments", "Level 3", "LB", "a.pdf"), local);
        }

        [TestMethod]
        public void PathsThatClimbOutOfTheLicenceFolder_AreRejected()
        {
            var storage = CreateStorage();

            Assert.ThrowsException<ArgumentException>(() => storage.GetLocalPath("569-829/../../../web.config"));
        }

        [TestMethod]
        public void BlobOnButNoConnectionString_FallsBackToLocalDisk()
        {
            var storage = CreateStorage(new GuardDocumentStorageOptions { UseBlobStorage = true });

            storage.Save(GuardDocumentStorage.RelativePath("569-829", "a.pdf"), new MemoryStream(Encoding.UTF8.GetBytes("x")));

            Assert.IsFalse(storage.IsBlobEnabled);
            Assert.IsTrue(File.Exists(LicenseDir("569-829", "a.pdf")));
        }

        [TestMethod]
        public void BlobOn_UsesTheExistingAzureStorageConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("AzureStorage:AzureStorageConnectionString", "UseDevelopmentStorage=true")
                })
                .Build();

            var storage = CreateStorage(new GuardDocumentStorageOptions { UseBlobStorage = true }, configuration);

            Assert.IsTrue(storage.IsBlobEnabled);
        }

        [TestMethod]
        public async Task Middleware_BlobOff_LeavesTheRequestToStaticFiles()
        {
            var storage = CreateStorage();
            var nextCalled = false;
            var middleware = new GuardDocumentBlobMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/Uploads/Guards/License/569-829/missing.pdf";

            await middleware.InvokeAsync(context, storage);

            Assert.IsTrue(nextCalled);
        }

        [TestMethod]
        public async Task Middleware_LocalFileExists_LeavesTheRequestToStaticFiles()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("AzureStorage:AzureStorageConnectionString", "UseDevelopmentStorage=true")
                })
                .Build();
            var storage = CreateStorage(new GuardDocumentStorageOptions { UseBlobStorage = true, KeepLocalCopy = true }, configuration);
            Directory.CreateDirectory(LicenseDir("569-829"));
            File.WriteAllText(LicenseDir("569-829", "here.pdf"), "x");

            var nextCalled = false;
            var middleware = new GuardDocumentBlobMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/Uploads/Guards/License/569-829/here.pdf";

            await middleware.InvokeAsync(context, storage);

            // served from disk, Blob never contacted
            Assert.IsTrue(nextCalled);
        }

        [TestMethod]
        public async Task Middleware_IgnoresOtherFolders()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("AzureStorage:AzureStorageConnectionString", "UseDevelopmentStorage=true")
                })
                .Build();
            var storage = CreateStorage(new GuardDocumentStorageOptions { UseBlobStorage = true }, configuration);
            var nextCalled = false;
            var middleware = new GuardDocumentBlobMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/Uploads/RosterCovers/a.png";

            await middleware.InvokeAsync(context, storage);

            Assert.IsTrue(nextCalled);
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public TestWebHostEnvironment(string webRootPath)
            {
                WebRootPath = webRootPath;
                ContentRootPath = webRootPath;
                WebRootFileProvider = new PhysicalFileProvider(webRootPath);
                ContentRootFileProvider = WebRootFileProvider;
            }

            public string WebRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string ApplicationName { get; set; } = "CityWatch.Web.Tests";
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
