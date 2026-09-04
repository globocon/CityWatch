

ALTER TABLE [ClientSiteKpiSettings]
ADD KpiTelematicsAndStatistics bit default 1 not null,
 SmartWandPatrolReports bit default 1 not null,
 MonthlyClientReport bit default 0 not null

 GO

 update  [ClientSiteKpiSettings]
   set KpiTelematicsAndStatistics = 1,
   SmartWandPatrolReports = 1,
   MonthlyClientReport = 0

   GO

