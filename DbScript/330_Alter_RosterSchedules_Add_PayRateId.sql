ALTER TABLE [dbo].[RosterSchedules] ADD [PayRateId] int NULL;
GO

ALTER TABLE [dbo].[RosterSchedules] WITH CHECK ADD CONSTRAINT [FK_RosterSchedules_PayRates_PayRateId] FOREIGN KEY([PayRateId])
REFERENCES [dbo].[PayRates] ([Id])
GO

ALTER TABLE [dbo].[RosterSchedules] CHECK CONSTRAINT [FK_RosterSchedules_PayRates_PayRateId]
GO
