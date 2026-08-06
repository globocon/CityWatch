-- Per-KVL docket lookup: docket download endpoint, generator upsert check
-- (GetKeyVehicleLogsDocketsHistory), and the join in the docket report
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KeyVehicleLogDocketHistory_KeyVehicleLogId' AND object_id = OBJECT_ID('dbo.KeyVehicleLogDocketHistory'))
CREATE NONCLUSTERED INDEX IX_KeyVehicleLogDocketHistory_KeyVehicleLogId
    ON KeyVehicleLogDocketHistory (KeyVehicleLogId)
    INCLUDE (DocketSerialNo);

-- Serial number search in the docket report
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KeyVehicleLogDocketHistory_DocketSerialNo' AND object_id = OBJECT_ID('dbo.KeyVehicleLogDocketHistory'))
CREATE NONCLUSTERED INDEX IX_KeyVehicleLogDocketHistory_DocketSerialNo
    ON KeyVehicleLogDocketHistory (DocketSerialNo)
    INCLUDE (KeyVehicleLogId);