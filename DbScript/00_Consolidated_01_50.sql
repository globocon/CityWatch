/** 
	--// Drop and create database fresh
	USE tempdb;
	GO
	DECLARE @SQL nvarchar(1000);
	IF EXISTS (SELECT 1 FROM sys.databases WHERE [name] = N'CityWatchDb')
	BEGIN
		SET @SQL = N'USE [CityWatchDb];

					 ALTER DATABASE CityWatchDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
					 USE [tempdb];

					 DROP DATABASE CityWatchDb;';
		EXEC (@SQL);
	END;
	CREATE DATABASE CityWatchDb

 
	--// After running all scripts, reset cwsadmin password to "a" (a lowercase)
	UPDATE Users set Password = '/IZm9lqxYju+e9bcwJXv7g==' where UserName = 'cwsadmin'
**/

-- Consolidated scripts from 01 to 36

USE CityWatchDb
GO

CREATE TABLE [dbo].[Users] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [UserName] VARCHAR (25)  NOT NULL,
    [Password] VARCHAR (MAX) NOT NULL,
    [IsAdmin]  BIT           NOT NULL
);
GO

CREATE TABLE [dbo].[ClientTypes](
	[Id] 	[int] 			IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Name] 	[varchar](75) 	NOT NULL
)
GO

CREATE TABLE [dbo].[ClientSites](
	[Id] 		[int] 			IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TypeId] 	[int] 			NOT NULL FOREIGN KEY REFERENCES ClientTypes (Id) ON DELETE CASCADE,
	[Name] 		[varchar](75) 	NOT NULL
)
GO

INSERT INTO [dbo].[Users] ([UserName], [Password], [IsAdmin]) VALUES (N'cwsadmin', N'TilPAut5HQMjHTu8boKB9ogtust8lQnljXxwPP7QuRQ=:cuLt/151p25y8hvfIUDRKw==', 1);
GO


DELETE FROM ClientSites;
GO

DELETE FROM ClientTypes;
GO

INSERT INTO ClientTypes VALUES ('Automotive');
INSERT INTO ClientTypes VALUES ('Health & Hospitals');
INSERT INTO ClientTypes VALUES ('Hotels & Accomodation')
INSERT INTO ClientTypes VALUES ('Industrial Sites')
INSERT INTO ClientTypes VALUES ('Local Council & Leisure Centre')
INSERT INTO ClientTypes VALUES ('Major Events & Concerts')
INSERT INTO ClientTypes VALUES ('Mobile Patrol Car (Adhoc)')
INSERT INTO ClientTypes VALUES ('Pubs & Nightclubs')
INSERT INTO ClientTypes VALUES ('Retail, Jewellery & Fashion Store')
INSERT INTO ClientTypes VALUES ('Shopping Centres & Markets')
INSERT INTO ClientTypes VALUES ('Schools, Library, & Educational')
INSERT INTO ClientTypes VALUES ('Transport Company')
INSERT INTO ClientTypes VALUES ('VISY-QLD')
INSERT INTO ClientTypes VALUES ('VISY-NSW')
INSERT INTO ClientTypes VALUES ('VISY-VIC')
INSERT INTO ClientTypes VALUES ('Zoo & Animals')
INSERT INTO ClientTypes VALUES ('Other, ADHOC, & Private Clients')
INSERT INTO ClientTypes VALUES ('N/A')
GO

INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Automotive'), 'Nunawading Hyundai');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Automotive'), 'Pickles Car Auctions');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Austin-Heidelberg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Camms Road Maternal & Child Health');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Royal Children''s Hospital');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Mercy-Heidelberg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Mercy-Werribee');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Hotels & Accomodation'), 'CitiPlan');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Consolidated Metals');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'CSR Gyprock');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Hoffman Brickworks');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Norstar Steel Recyclers');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Victorian Chemical');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'City of Casey');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'Cranbourne Leisure Centre');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'Endeavour Hills Leisure Centre');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'Avalon AirShow');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'F1 GrandPrix');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'Melbourne Cup');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'New Years Eve');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Bulgari');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Channel');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Crown Casino');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Leonard Joel');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'L''Oreal');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Revlon');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Caribbean Gardens');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Chadstone Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Churinga Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Dalton Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Ferntree Plaza Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Highpoint Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Hive Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Milleara Mall');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Niddrie Central Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Doveton Library');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Melbourne University');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Notre Dame Uni');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Polytechnic');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Vermont South Special School');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Arrow');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Chemtrans');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Jayco Caravans');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Mainfreight');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Carrara');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Gibson Island');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Maroochydore');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Rocklea');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Albury');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Smithfield');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Warrick Farm');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Wollongong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Banyule');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Clayton');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Coolaroo');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Coburg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Dandenong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Geelong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Kilsyth');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Laverton');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Reservoir');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Springvale');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Truganina');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Lort-Smith Animal Hospital');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Melbourne Zoo');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Werribee Zoo');
GO

CREATE TABLE [dbo].[FeedbackTemplates]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Name] VARCHAR(100) NOT NULL,
	[Text] VARCHAR(MAX) NULL
);
GO

CREATE TABLE [dbo].[ReportTemplates]
(
	[Id] INT NOT NULL PRIMARY KEY,
	[Path] VARCHAR(1024) NOT NULL,
	[LastUpdated] DATETIME NOT NULL
);
GO

INSERT INTO ReportTemplates VALUES (1, '/wwwroot/Pdf/Template', GETDATE())
GO

INSERT INTO [FeedbackTemplates] VALUES ('DATA Error - No Internet', 'I started my shift @ 00:00 hrs on YYYYMMDD.
I noticed @ 00:00 hrs we have experienced and internet connection issue.
The FLIR images are on the Laptop Dropbox; but we have no internet because the Smart WAND wont connect to cloud.');

INSERT INTO [FeedbackTemplates] VALUES ('Emergency Services on Site', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed an Alarm sounding from 
I called 
I then
At approx. 00:00 hrs a fire truck arrived (number plate XXX-XXX) 
I then
At approx. 00:00 hrs a fire truck departed (number plate XXX-XXX) 
I then
I also took photos of the panel / fire trucks / event');

INSERT INTO [FeedbackTemplates] VALUES ('Fire Equipment - Alarm', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed an Alarm sounding from the ASE / FIP / VESDA which is located in
I then
I also took a photo of the');

INSERT INTO [FeedbackTemplates] VALUES ('Fire Equipment - Fault', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed a Fault or Error on the ASE / FIP / VESDA which is located in
I then
I also took a photo of the');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – EXT. Only - All ok', 'An External patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – INT. Only - All ok', 'An internal patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – EXT + INT Only - All ok', 'Both an external and an internal patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol - CLIENT on site', 'I started my shift @ 00:00 hrs on YYYYMMDD.
The client is working on site and hence we can not conduct a full patrol.
The client left @ 00:00 and we then continued our shift like normal.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol – Inclement Weather - Hot', 'I started my shift @ 00:00 hrs on YYYYMMDD.
It was an extremely hot day, the temperature was XX oC
We reduced our patrols due to OH&S reasons however remained vigilant in case of any fire risks.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol – Inclement Weather - Raining', 'I started my shift @ 00:00 hrs on YYYYMMDD.
It was a very wet day, with heavy rain. 
We reduced our patrols due to OH&S reasons.');

GO

ALTER TABLE [dbo].[ClientSites]
ADD [Emails] VARCHAR (MAX) NULL
GO

ALTER TABLE [dbo].[ClientSites]
ADD [Address] VARCHAR (512) NULL,
    [State] VARCHAR (10) NULL
GO

ALTER TABLE [dbo].[ClientSites]
ADD [Billing] VARCHAR (25) NULL
GO

ALTER TABLE [dbo].[ClientSites]
ADD [Gps] VARCHAR (25) NULL
GO

ALTER TABLE [dbo].[ClientSites]
ALTER COLUMN [Gps] VARCHAR (75) NULL
GO

CREATE TABLE [dbo].IncidentReports (
    Id INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [FileName] VARCHAR(1024) NOT NULL,
    CreatedOn DATETIME NOT NULL,    
    ClientSiteId INT NULL FOREIGN KEY REFERENCES ClientSites(Id) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[UserClientSiteAccess]
(
	Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
	ClientSiteId INT NOT NULL FOREIGN KEY REFERENCES ClientSites(Id) ON DELETE CASCADE
)
GO

ALTER TABLE Users ADD CONSTRAINT UQ_User_Name UNIQUE (UserName)
GO

UPDATE [Users] set [Password] = 'O7RtFd3W1UEAm9/sAqGvQg==' where [UserName] = 'cwsadmin'


CREATE TABLE [dbo].[StaffDocuments]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY ,
	[FileName] VARCHAR(1024) NOT NULL,
	[LastUpdated] DATETIME NOT NULL
);
GO

CREATE TABLE [dbo].[DailyClientSiteKpis]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]) ON DELETE CASCADE,
	[Date] [date] NOT NULL,
	[EmployeeHours] [decimal](18, 0) NULL,
	[ImageCount] [int] NULL,
	[WandScanCount] [int] NULL,
	[IncidentCount] [int] NULL
)
GO

CREATE TABLE [dbo].[KpiDataImportJobs]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]) ON DELETE CASCADE,
	[ReportDate] [date] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CompletedDate] [datetime] NULL,
	[Success] [bit] NULL
)
GO

CREATE TABLE [dbo].[ClientSiteKpiSettings]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]) ON DELETE CASCADE,
	[KoiosClientSiteId] [int] NULL,
	[DropboxImagesDir] VARCHAR (1024) NULL,
	[DailyImagesTarget] DECIMAL(5,2) NULL,
	[DailyWandScansTarget] DECIMAL(5,2) NULL,	
	[MonEmpHrs] DECIMAL(5, 2) NULL,
	[TueEmpHrs] DECIMAL(5, 2) NULL,
	[WedEmpHrs] DECIMAL(5, 2) NULL,
	[ThuEmpHrs] DECIMAL(5, 2) NULL,
	[FriEmpHrs] DECIMAL(5, 2) NULL,
	[SatEmpHrs] DECIMAL(5, 2) NULL,
	[SunEmpHrs] DECIMAL(5, 2) NULL
)
GO

ALTER TABLE [dbo].[ClientSites]
ADD [Status] INT NOT NULL DEFAULT 0,
	[StatusDate] DATE NULL
GO

ALTER TABLE [dbo].[ClientSiteKpiSettings]
ADD [PhotoPointsPerPatrol] INT NULL,
    [WandPointsPerPatrol] INT NULL,
    [PatrolsPerHour] INT NULL,
    [TuneDowngradeBuffer] DECIMAL(5, 2) NULL,	 
    [IsThermalCameraSite] BIT NOT NULL DEFAULT 0,
    [SiteImage] VARCHAR(1024) NULL
GO

ALTER TABLE ClientTypes ADD CONSTRAINT UQ_Client_Type_Name UNIQUE ([Name])
GO

ALTER TABLE ClientSites ADD CONSTRAINT UQ_Client_Site_Name UNIQUE ([Name])
GO

ALTER TABLE [dbo].[ClientSiteKpiSettings]
ADD [ExpPatrolDuration] INT NULL,
    [MinPatrolFreq] INT NULL,
	[MinImagesPerPatrol] INT NULL,
	[Notes] VARCHAR(MAX) NULL
GO

CREATE TABLE [dbo].[IncidentReportFields]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TypeId] [INT] NOT NULL,
	[Name] VARCHAR(50) NOT NULL,
	[EmailTo] VARCHAR(1024) NULL,
	CONSTRAINT UQ_IncidentReportField UNIQUE([TypeId], [Name])
)
GO

-- Adding Officer Positions

INSERT INTO IncidentReportFields values(1,'Fire Surveillance Officer (FSO)', null);
INSERT INTO IncidentReportFields values(1,'COVID Marshall', null);
INSERT INTO IncidentReportFields values(1,'Security - Armed (CIT)', null);
INSERT INTO IncidentReportFields values(1,'Security - Bodyguard', null);
INSERT INTO IncidentReportFields values(1,'Security - Crowd Controller', null);
INSERT INTO IncidentReportFields values(1,'Security - Control Room', null);
INSERT INTO IncidentReportFields values(1,'Security - Hospital Guard', null);
INSERT INTO IncidentReportFields values(1,'Security - Static / Site Guard', null);
INSERT INTO IncidentReportFields values(1,'Security - Gatehouse Guard', null);
INSERT INTO IncidentReportFields values(1,'Security - Mobile Patrols (Car) M1', 'patrols@citywatchsecurity.com.au');
INSERT INTO IncidentReportFields values(1,'Security - Mobile Patrols (Car) X1', null);
INSERT INTO IncidentReportFields values(1,'Security - Shift Supervisor', null);
INSERT INTO IncidentReportFields values(1,'Security - Site Supervisor', null);
INSERT INTO IncidentReportFields values(1,'Citywatch Director', null);
INSERT INTO IncidentReportFields values(1,'Citywatch Project Manager', null);
INSERT INTO IncidentReportFields values(1,'Investigator / Auditor', null);
INSERT INTO IncidentReportFields values(1,'Other', null);

-- Adding Notified By
INSERT INTO IncidentReportFields values(2,'ACOST', null);
INSERT INTO IncidentReportFields values(2,'AMSEC', null);
INSERT INTO IncidentReportFields values(2,'CWS-Gerald', null);
INSERT INTO IncidentReportFields values(2,'CWS-Sharon', null);
INSERT INTO IncidentReportFields values(2,'CWS-Richard', null);
INSERT INTO IncidentReportFields values(2,'CWS-Rocky', null);
INSERT INTO IncidentReportFields values(2,'No One (Myself)', null);
INSERT INTO IncidentReportFields values(2,'SAMS', null);
INSERT INTO IncidentReportFields values(2,'OZLAND', null);
INSERT INTO IncidentReportFields values(2,'Client', null);

--Adding CallSign
INSERT INTO IncidentReportFields values(3,'R1', null);
INSERT INTO IncidentReportFields values(3,'R2', null);
INSERT INTO IncidentReportFields values(3,'R3', null);
INSERT INTO IncidentReportFields values(3,'R4', null);
INSERT INTO IncidentReportFields values(3,'R5', null);
GO

ALTER TABLE [dbo].[ClientSiteKpiSettings]
ADD [IsWeekendOnlySite] BIT NOT NULL DEFAULT 0
GO

ALTER TABLE [dbo].[IncidentReports]
ADD IncidentDateTime dateTime NULL,	
	ReportDateTime dateTime NULL,	
	JobNumber varchar(15) NULL,
	JobTime char(5) NULL,	
	CallSign varchar(15) NULL,
	NotifiedBy varchar(25) NULL,
	Billing varchar(25) NULL
GO

ALTER TABLE [dbo].[KpiDataImportJobs]
ADD [StatusMessage] VARCHAR(MAX) NULL
GO

ALTER TABLE [dbo].[IncidentReports]
ADD [IsEventFireOrAlarm] BIT NOT NULL DEFAULT 0
GO

ALTER TABLE [dbo].[DailyClientSiteKpis]
ADD [FireOrAlarmCount] [int] NULL
GO

ALTER TABLE [dbo].[IncidentReports]
ADD [OccurNo] VARCHAR(15) NULL,
	[ActionTaken] VARCHAR(MAX) NULL,
	[IsPatrol] BIT NOT NULL DEFAULT 0,
	[Position] VARCHAR(50) NULL
GO

ALTER TABLE [dbo].[IncidentReports]
ADD ClientArea VARCHAR(256) NULL
GO

ALTER TABLE [dbo].[FeedbackTemplates]
ADD [Type] INT NOT NULL DEFAULT 1
GO

CREATE TABLE [dbo].[ClientSiteDayKpiSettings]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	SettingsId INT NOT NULL FOREIGN KEY References ClientSiteKpiSettings(Id) ON DELETE CASCADE,
	[WeekDay] INT NOT NULL,
	PatrolFrequency INT NOT NULL Default 0,
	NoOfPatrols INT NULL,
	EmpHours DECIMAL(5,2) NULL,
	ImagesTarget DECIMAL(5,2) NULL,
	WandScansTarget DECIMAL(5,2) NULL
)
GO

CREATE TABLE dbo.KpiSendSchedules
(
	Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	ClientSiteId INT NOT NULL,
	StartDate DATE NOT NULL,
	EndDate DATE NOT NULL,	
	Frequency INT NOT NULL,
	[Time] CHAR(5),
	EmailTo VARCHAR(5000) NULL,
	NextRunOn DATETIME NOT NULL
)
GO

CREATE TABLE TempDaySettings
(
	SettingsId INT,
	[WeekDay] INT,
	NoOfPatrols INT,
	EmpHours decimal(5,2),
	ImagesTarget decimal(5,2),
	WandScansTarget decimal(5,2)
)
GO

INSERT INTO TempDaySettings 
SELECT Id, 1, PatrolsPerHour,  MonEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 2, PatrolsPerHour,  TueEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 3, PatrolsPerHour,  WedEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 4, PatrolsPerHour,  ThuEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 5, PatrolsPerHour,  FriEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 6, PatrolsPerHour,  SatEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 0, PatrolsPerHour,  SunEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
GO

INSERT INTO [ClientSiteDayKpiSettings] (SettingsId, [WeekDay], NoOfPatrols, EmpHours, ImagesTarget, WandScansTarget )
SELECT SettingsId, [WeekDay], NoOfPatrols, EmpHours, ImagesTarget, WandScansTarget FROM TempDaySettings ORDER BY SettingsId, [WeekDay]
GO

DROP TABLE TempDaySettings
GO

CREATE TABLE dbo.KpiSendScheduleJobs
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	CreatedDate DateTime NOT NULL,
	CompletedDate DateTime NULL,
	Success BIT NULL,
	StatusMessage VARCHAR(max) NULL
)
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ALTER COLUMN [EndDate] date null
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ALTER COLUMN [ClientSiteId] int null
GO


CREATE TABLE dbo.KpiSendScheduleClientSites
(
	Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	ScheduleId INT NOT NULL FOREIGN KEY REFERENCES KpiSendSchedules(Id) ON DELETE CASCADE,
	ClientSiteId INT NOT NULL FOREIGN KEY REFERENCES ClientSites(Id) ON DELETE CASCADE
)
GO

INSERT INTO [KpiSendScheduleClientSites]
SELECT Id, ClientSiteId FROM KpiSendSchedules
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ADD IsPaused BIT NOT NULL DEFAULT 0
GO

ALTER TABLE [dbo].[IncidentReports]
ADD SerialNo VARCHAR(50) NULL	
GO

CREATE TABLE dbo.AppConfigurations
(
	Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Name]  VARCHAR(50) NOT NULL,
	[Value] VARCHAR(1024) NOT NULL,
	UNIQUE ([Name])
)
GO

INSERT INTO dbo.AppConfigurations VALUES('LastUsedIrSn', '0')
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ADD [ProjectName] VARCHAR(50) NULL,
[SummaryNote1] VARCHAR(2048) NULL,
[SummaryNote2] VARCHAR(2048) NULL

CREATE UNIQUE NONCLUSTERED INDEX UQ_Ir_SerialNo
ON dbo.IncidentReports(SerialNo)
WHERE SerialNo IS NOT NULL
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_KpiSchedules_ProjectName
ON dbo.KpiSendSchedules(ProjectName)
WHERE ProjectName IS NOT NULL
GO

DROP INDEX [UQ_Ir_SerialNo] ON [dbo].[IncidentReports]
GO

