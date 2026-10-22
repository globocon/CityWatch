# HR documents → Azure Blob (Issue 370, Part 2)

Branch: `feature/P1_Issue_370_Part_2_HRDocs_Blob`

## 1. What changed, in simple words

Before this change, every guard HR document was saved in one folder on the VM:
`wwwroot/Uploads/Guards/License/{SecurityNo}/...`. This covers licences, compliance and HR files,
practical and RPL evidence, and generated training certificates.

Now all HR document saving and reading goes through one new class, `GuardDocumentStorage`. It can save to
Azure Blob (container `guard-docs`), to the VM disk, or to both. A config switch controls which.
**The switch is OFF by default**, so after deploying this code the system behaves exactly as it does today
until production is switched over.

| Where | Change |
|---|---|
| `Services/GuardDocumentStorage.cs` (new) | Save, read and publish HR documents to disk and/or Blob |
| `Services/GuardDocumentBlobMiddleware.cs` (new) | Keeps the old `/Uploads/Guards/License/...` links working |
| `Pages/Admin/hr.cs` | Web uploads (HR docs, practical evidence, RPL evidence single and bulk) and the Dropbox copy go through the new class |
| `Services/ViewDataService.cs` | Mobile app upload (`api/GuardSecurityNumber/SaveHrRecordOfGuard`) and its Dropbox copy |
| `Services/CertificateGenerator.cs` | Reads evidence files from disk or Blob; sends the finished certificate to Blob |
| `Program.cs`, `appsettings.json` | Registers the service and middleware; new `GuardDocumentStorage` section |

Not changed:
- Database: no script is needed. The DB stores only the file name, never a path.
- JavaScript in Web and RadioCheck, and the mobile app.
- Dropbox: every HR document is still copied to Dropbox as before.
- CityWatch.Kpi: it never reads these files.

## 2. How existing files work after the move

Web pages, RadioCheck (`https://cws-ir.com/Uploads/...`) and the mobile app all keep the same link:
`/Uploads/Guards/License/{SecurityNo}/{FileName}`.

When that link is opened, the new middleware does this:
1. **If the file is still on the VM disk**, it serves it from disk, exactly as today.
2. **If the file is not on disk but is in Blob**, it redirects the browser to a private Blob link that expires after 15 minutes.
3. **If the file is in neither**, it returns 404, as today.

This means files can be copied to Blob and removed from the VM gradually without breaking any link.
The mobile app opens the link in the phone's browser, which follows the redirect, so **no new app build is needed**.

## 3. How new files work

| Settings | What happens to a new upload or certificate |
|---|---|
| `UseBlobStorage: false` (default) | Saved to VM disk only, as today |
| `UseBlobStorage: true`, `KeepLocalCopy: true` | Saved to VM disk **and** Blob (safe first stage) |
| `UseBlobStorage: true`, `KeepLocalCopy: false` | Saved to Blob only. The VM is used only as a short-lived working area |

Reading new files:
- Code that needs a real file, such as the Dropbox upload or merging evidence into a certificate PDF, gets
  a temporary local copy downloaded from Blob. That copy is deleted straight after use.
- If a Blob upload fails, the file is **kept on the VM disk** and the error is logged, so no document is lost.
  The link still works because the disk is checked first.

The container `guard-docs` is **private**: no public access. Today anyone with a link can download HR
documents from `wwwroot`. Once the files are only in Blob, the 15-minute link is the only way in.
(The `/Uploads/...` link itself is still unauthenticated, as before. Putting a login check on it is a
possible next step.)

## 4. Production server steps

Storage account: the one already in `AzureStorage:AzureStorageConnectionString` (`c4istorage1`).
A different account can be used by setting `GuardDocumentStorage:ConnectionString`.

**Step 1: Deploy the code.** Leave `UseBlobStorage` set to `false`. Nothing changes for users.

**Step 2: Create the container.** In the Azure portal, open Storage account → Containers → **+ Container**.
Name it `guard-docs` and set access level to **Private**. (The app also creates it on its first upload, but
creating it by hand first is recommended.)

**Step 3: Turn on writing to both places.** In the production `appsettings.json` set:
```json
"GuardDocumentStorage": {
  "UseBlobStorage": true,
  "ContainerName": "guard-docs",
  "KeepLocalCopy": true,
  "LinkExpiryMinutes": 15
}
```
**Recycle the IIS app pool**, because settings are read at start-up.
From now on, new files go to both disk and Blob.

**Step 4: Copy the existing files with AzCopy.**
1. Install AzCopy v10.
2. Create a container SAS in the portal: Container → Shared access tokens, with permissions Read, Add,
   Create, Write and List, valid for 1 day.
3. Run:
```bash
azcopy copy "D:\<site>\wwwroot\Uploads\Guards\License" "https://c4istorage1.blob.core.windows.net/guard-docs?<SAS>" --recursive --overwrite=ifSourceNewer
```
This creates blobs named `License/{SecurityNo}/...`, which is the layout the code expects.

To check that everything was copied, run the command below. It should report nothing left to copy:
```bash
azcopy sync "D:\<site>\wwwroot\Uploads\Guards\License" "https://c4istorage1.blob.core.windows.net/guard-docs/License?<SAS>" --recursive --dry-run
```

**Step 5: Test with Blob.**
1. Rename the folder `wwwroot\Uploads\Guards\License` to `License_backup_yyyyMMdd`. **Do not delete it.**
2. Open a guard's HR records in Web, RadioCheck and the mobile app, and confirm the documents open.
3. Upload a document, then generate a certificate.
To roll back at this stage, rename the folder back.

**Step 6: Stop keeping copies on the VM.**
1. Set `"KeepLocalCopy": false` and recycle the app pool.
2. New files now go to Blob only.
3. After about 30 days with no problems, delete `License_backup_yyyyMMdd`.

**Rollback at any point:**
1. Set `"UseBlobStorage": false` and recycle the app pool.
2. If Step 6 had already been done, first copy the Blob-only files back to the VM:
```bash
azcopy copy "https://c4istorage1.blob.core.windows.net/guard-docs/License?<SAS>" "D:\<site>\wwwroot\Uploads\Guards" --recursive --overwrite=ifSourceNewer
```

**Other checks:**
- The IIS `web.config` must keep sending every request to the ASP.NET Core module, as it does today.
  If an IIS static-file handler were added for `/Uploads`, the redirect middleware would never run.
- Blob names are case-sensitive and Windows file names are not. Links are built from the same `SecurityNo`
  and `FileName` that were used when saving, so they match. A document whose file name was changed on the VM
  by hand, with different letter case, would not be found in Blob.

## 5. Still to move to Blob (later parts)

Out of scope: JotForm files (`uploads/jotform/...`, webhooks and IFrame pages). They are not part of this project and are not touched.

| Folder | Main writer | Notes |
|---|---|---|
| `Uploads/{ReportReference}/` (IR attachments) | `Pages/Incident/Register.cshtml.cs`, mobile API | Read by `IncidentReportGenerator` |
| `KvlUploads/...` (KVL images and compliance docs) | `Pages/Guard/KeyVehicleLog.cshtml.cs` | Also read by Kpi `KeyVehicleGenerator` |
| `DglUploads/{guardLogId}/...` (daily log images) | `Pages/Guard/DailyLog.cshtml.cs`, mobile API | |
| `Uploads/RosterCovers/...` | `Pages/roster/Booking.cshtml.cs` | Read by `RosterReportGenerator` |
| `TA/{HRref}/{Course,Certificate}` | `Pages/Admin/Settings.cshtml.cs` | Course docs and certificate **templates** |
| `TA/Feedback/{SecurityNo}` | `CertificateGenerator.cs` | Guard feedback PDFs (HR-adjacent) |
| `StaffDocs` | Web and Kpi `Settings.cshtml.cs` | |
| `DuressAppAudio`, `DuressAppMultimedia` | `Pages/Admin/hr.cs` | |
| `AudioRecordings` | RadioCheck `Record.cshtml.cs` | |
| `SubdomainLogo`, `Images`, Kpi `RCImage`/`KeyImage`/`SummaryImage`/`SiteImage` | Settings pages | Small; low priority |

Should stay on the VM (generated or temporary): `Pdf/{Output,Archive,ToDropbox,FromDropbox,Template}`,
`Excel/Output`, `GpsImage*` and `GraphImage`.

Known issues found along the way, not changed:
- Deleting an HR record removes only the DB row. The file stays on disk, and now in Blob too, as it did before.
- `hr.cs` `UploadGuardLicenseToDropbox` and `UploadGuardComplianceToDropbox` read from `Uploads/Guards/{GuardId}`,
  which nothing writes to (old tables). They were already failing silently.
- `CertificateGenerator` copies non-expiry certificates to the old Dropbox folder (`GetGuardDocumentDbxRootFolder`)
  and expiry certificates to the new one.
