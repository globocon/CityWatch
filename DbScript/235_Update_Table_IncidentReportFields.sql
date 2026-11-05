select * from IncidentReportFields where clientsiteids is not null



declare @valuenewint int=(SELECT    distinct TypeId as CarNames
 FROM ClientSites WHERE Id in  (390,659))
		
declare @value  nvarchar(max)=(SELECT    STRING_AGG( @valuenewint,';') as CarNames)
		

		update IncidentReportFields set ClientTypeIds=@value where ClientSiteIds='390;659'

declare @valuenewint1 int=(SELECT    distinct TypeId as CarNames
 FROM ClientSites WHERE Id in  (470,485,487,488,486,499,492,665,7))

	declare @valuenew  nvarchar(max)=(SELECT    STRING_AGG( @valuenewint1,';') as CarNames)
	
		update IncidentReportFields set ClientTypeIds=@valuenew where ClientSiteIds='470;485;487;488;486;499;492;665;7'

		


