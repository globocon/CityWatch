CREATE TABLE [dbo].[TrainingLocation](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Location] [varchar](max) NULL,
	[IsDeleted] [bit] NULL default 0,
 CONSTRAINT [PK_TrainingLocation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into TrainingLocation(Location)values('Online')