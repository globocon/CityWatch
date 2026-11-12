USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardLogins]    Script Date: 04-02-2026 10:00:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardLoginSmartWandUse](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[GuardLoginId] [int] NOT NULL,
	
	[SmartWandId] [int] NULL,
	
	[IPAddress] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

