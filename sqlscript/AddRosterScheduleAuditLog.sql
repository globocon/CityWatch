SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RosterScheduleAuditLogs] (
    [Id]               INT            IDENTITY (1, 1) NOT NULL,
    [RosterScheduleId] INT            NOT NULL,
    [ActionTime]       DATETIME2 (7)  NOT NULL,
    [UserId]           INT            NULL,
    [GuardId]          INT            NULL,
    [ActionSource]     NVARCHAR (20)  NULL,
    [Action]           NVARCHAR (50)  NULL,
    [Details]          NVARCHAR (MAX) NULL,
    [IPAddress]        NVARCHAR (50)  NULL,
    [Platform]         NVARCHAR (100) NULL,
    [OldStatus]        INT            NULL,
    [NewStatus]        INT            NULL,
    CONSTRAINT [PK_RosterScheduleAuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RosterScheduleAuditLogs_RosterSchedules_RosterScheduleId] FOREIGN KEY ([RosterScheduleId]) REFERENCES [dbo].[RosterSchedules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RosterScheduleAuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [FK_RosterScheduleAuditLogs_Guards_GuardId] FOREIGN KEY ([GuardId]) REFERENCES [dbo].[Guards] ([Id])
);
GO

CREATE INDEX [IX_RosterScheduleAuditLogs_RosterScheduleId] ON [dbo].[RosterScheduleAuditLogs]([RosterScheduleId] ASC);
GO

CREATE INDEX [IX_RosterScheduleAuditLogs_UserId] ON [dbo].[RosterScheduleAuditLogs]([UserId] ASC);
GO

CREATE INDEX [IX_RosterScheduleAuditLogs_GuardId] ON [dbo].[RosterScheduleAuditLogs]([GuardId] ASC);
GO
