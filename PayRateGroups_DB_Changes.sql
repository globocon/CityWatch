
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 1. Create PayRateGroups table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PayRateGroups]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PayRateGroups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[IsDeleted] [bit] NOT NULL DEFAULT ((0)),
 CONSTRAINT [PK_PayRateGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- 2. Add PayRateGroupId to PayRates table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PayRates]') AND name = 'PayRateGroupId')
BEGIN
    ALTER TABLE [dbo].[PayRates] ADD [PayRateGroupId] [int] NULL;
END
GO

-- 3. Add Foreign Key if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PayRates_PayRateGroups]') AND parent_object_id = OBJECT_ID(N'[dbo].[PayRates]'))
BEGIN
    ALTER TABLE [dbo].[PayRates]  WITH CHECK ADD  CONSTRAINT [FK_PayRates_PayRateGroups] FOREIGN KEY([PayRateGroupId])
    REFERENCES [dbo].[PayRateGroups] ([Id])
END
GO

ALTER TABLE [dbo].[PayRates] CHECK CONSTRAINT [FK_PayRates_PayRateGroups]
GO
