alter table KpiSendSchedules
add IsCriticalDocumentDownselect bit

alter table KpiSendSchedules
add CriticalGroupNameID varchar(100)



update KpiSendSchedules set IsCriticalDocumentDownselect=0