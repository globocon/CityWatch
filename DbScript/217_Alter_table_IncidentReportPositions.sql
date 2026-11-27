

alter table IncidentReportPositions
add IsLogbook bit

UPDATE IncidentReportPositions SET IsLogbook = 0 WHERE IsLogbook IS NULL;