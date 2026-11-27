
CREATE TABLE [dbo].[PayRates] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(max) NOT NULL,
    [SellRateToClient] decimal(18,2) NOT NULL,
    [Comms1] decimal(18,2) NOT NULL,
    [Comms2] decimal(18,2) NOT NULL,
    [GuardPayRate] decimal(18,2) NOT NULL,
    [Currency] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_PayRates] PRIMARY KEY ([Id])
);
