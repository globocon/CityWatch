-- Create the new ShiftCancellationEmailQueues table
CREATE TABLE [dbo].[ShiftCancellationEmailQueues] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [GuardId] INT NOT NULL,
    [ClientSiteId] INT NOT NULL,
    [ShiftStart] DATETIME2 NOT NULL,
    [ShiftEnd] DATETIME2 NOT NULL,
    [Reason] NVARCHAR(MAX) NULL,
    [CancelledBy] NVARCHAR(100) NULL,
    [Source] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [IsProcessed] BIT NOT NULL,
    CONSTRAINT [PK_ShiftCancellationEmailQueues] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ShiftCancellationEmailQueues_Guards_GuardId] FOREIGN KEY ([GuardId]) REFERENCES [dbo].[Guards] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ShiftCancellationEmailQueues_ClientSites_ClientSiteId] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
);
GO

-- Create index for quick lookups by GuardId
CREATE NONCLUSTERED INDEX [IX_ShiftCancellationEmailQueues_GuardId] 
    ON [dbo].[ShiftCancellationEmailQueues] ([GuardId]);
GO

-- Create index for quick lookups by ClientSiteId
CREATE NONCLUSTERED INDEX [IX_ShiftCancellationEmailQueues_ClientSiteId] 
    ON [dbo].[ShiftCancellationEmailQueues] ([ClientSiteId]);
GO

-- Create an index to speed up the background worker polling (finding unprocessed records)
CREATE NONCLUSTERED INDEX [IX_ShiftCancellationEmailQueues_IsProcessed_CreatedAt] 
    ON [dbo].[ShiftCancellationEmailQueues] ([IsProcessed], [CreatedAt]);
GO
