using CityWatch.Common.Models;
using Dropbox.Api;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Dropbox.Api.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Common.Services
{
    public interface IDropboxService
    {
        Task<bool> Upload(DropboxSettings settings, string fileToUpload, string dbxFilePath);

        Task<bool> Download(DropboxSettings settings, string downloadToFolder, string[] filesToDownload);       
    }

    public class DropboxService : IDropboxService
    {
        public async Task<bool> Upload(DropboxSettings settings, string fileToUpload, string dbxFilePath)
        {
            using var dbxTeam = new DropboxTeamClient(settings.AccessToken, settings.RefreshToken, settings.AppKey, settings.AppSecret, new DropboxClientConfig());
            var team = dbxTeam.Team.MembersListAsync().Result;
            if (team.Members.Count > 0)
            {
                var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == settings.UserEmail);
                if (cwsMember != null)
                {
                    var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                    var account = await dbx.Users.GetCurrentAccountAsync();
                    var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                    await ChunkUpload(dbx, nsId, fileToUpload, dbxFilePath);
                }
            }
            return true;
        }

        public async Task<bool> Download(DropboxSettings settings, string downloadToFolder, string[] filesToDownload)
        {
            using var dbxTeam = new DropboxTeamClient(settings.AccessToken, settings.RefreshToken, settings.AppKey, settings.AppSecret, new DropboxClientConfig());
            var team = dbxTeam.Team.MembersListAsync().Result;
            if (team.Members.Count > 0)
            {
                var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == settings.UserEmail);
                if (cwsMember != null)
                {
                    var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                    var account = await dbx.Users.GetCurrentAccountAsync();
                    var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                    foreach (var fileToDownload in filesToDownload)
                    {
                        var fileName = Path.GetFileName(fileToDownload);

                        try
                        {
                            await Download(dbx, nsId, fileToDownload, Path.Combine(downloadToFolder, fileName));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return true;
        }

        private static async Task<bool> ChunkUpload(DropboxClient client, PathRoot.NamespaceId nsId, string srcFilePath, string destFilePath)
        {
            int chunkSize = 128 * 1024;       // Chunk size is 128KB.
            if (new FileInfo(srcFilePath).Length < chunkSize)
                chunkSize = 16 * 1024;

            using var stream = new MemoryStream(File.ReadAllBytes(srcFilePath));
            int numChunks = (int)Math.Ceiling((double)stream.Length / chunkSize);

            byte[] buffer = new byte[chunkSize];
            string sessionId = null;

            for (var index = 0; index < numChunks; index++)
            {
                var byteRead = stream.Read(buffer, 0, chunkSize);

                using var memStream = new MemoryStream(buffer, 0, byteRead);
                if (index == 0)
                {
                    var result = await client.WithPathRoot(nsId).Files.UploadSessionStartAsync(body: memStream);
                    sessionId = result.SessionId;
                }
                else
                {
                    var cursor = new UploadSessionCursor(sessionId, (ulong)(chunkSize * index));

                    if (index == numChunks - 1)
                        await client.WithPathRoot(nsId).Files.UploadSessionFinishAsync(cursor, new CommitInfo(destFilePath, mode: WriteMode.Overwrite.Instance), body: memStream);
                    else
                        await client.WithPathRoot(nsId).Files.UploadSessionAppendV2Async(cursor, body: memStream);
                }
            }

            return true;
        }

        private static async Task Download(DropboxClient client, PathRoot.NamespaceId nsId, string filePath, string localFilePath)
        {
            using var response = await client.WithPathRoot(nsId).Files.DownloadAsync(filePath);
            using var fileStream = File.Create(localFilePath);
            (await response.GetContentAsStreamAsync()).CopyTo(fileStream);
        }       
    }
}
