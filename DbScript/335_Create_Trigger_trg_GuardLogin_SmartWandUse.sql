
CREATE TRIGGER trg_GuardLogin_SmartWandUse
   ON  GuardLogins
   AFTER  INSERT,UPDATE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

   INSERT INTO GuardLoginSmartWandUse
    (
        GuardLoginId,
        SmartWandId,
		IPAddress,
        CreatedDate
    )
    SELECT
        i.Id,
        i.SmartWandId,
		i.IPAddress,
        GETDATE()
    FROM INSERTED i
    WHERE i.SmartWandId IS NOT NULL;

END
GO
