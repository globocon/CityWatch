-- Create table for Allowances with full rate fields
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Allowances]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Allowances] (
        [Id]                INT IDENTITY (1, 1) NOT NULL,
        [Description]       NVARCHAR (255)  NOT NULL,
        [FQ]                NVARCHAR (50)   NULL, -- Per hr, Per shift, Per day, Per week, Per Km
        [SellRateToClient]  DECIMAL (18, 2) DEFAULT ((0)) NOT NULL,
        [Comms1]            DECIMAL (18, 2) DEFAULT ((0)) NOT NULL,
        [Comms2]            DECIMAL (18, 2) DEFAULT ((0)) NOT NULL,
        [GuardPayRate]      DECIMAL (18, 2) DEFAULT ((0)) NOT NULL,
        [Currency]          NVARCHAR (10)   NULL,
        [IsDeleted]         BIT             DEFAULT ((0)) NOT NULL,
        [CreatedDate]       DATETIME        DEFAULT (getdate()) NOT NULL,
        [UpdatedDate]       DATETIME        NULL,
        CONSTRAINT [PK_Allowances] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO
