CREATE UNIQUE NONCLUSTERED INDEX UQ_Ir_SerialNo
ON dbo.IncidentReports(SerialNo)
WHERE SerialNo IS NOT NULL
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_KpiSchedules_ProjectName
ON dbo.KpiSendSchedules(ProjectName)
WHERE ProjectName IS NOT NULL
GO
