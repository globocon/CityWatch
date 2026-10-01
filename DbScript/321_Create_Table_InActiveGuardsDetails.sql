
CREATE TABLE [dbo].[InActiveGuardsDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NOT NULL,
	[LastWorkingDate] [datetime] NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))
