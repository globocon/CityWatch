
CREATE NONCLUSTERED INDEX IX_GuardComplianceLicense_GuardId_HrGroup ON dbo.GuardComplianceLicense (GuardId, HrGroup);
CREATE NONCLUSTERED INDEX IX_HrSettings_HRGroupId ON dbo.HrSettings (HRGroupId) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_HrSettingsClientSites_HrSettingsId ON dbo.HrSettingsClientSites (HrSettingsId);
CREATE NONCLUSTERED INDEX IX_HrSettingsClientStates_HrSettingsId ON dbo.HrSettingsClientStates (HrSettingsId);