DECLARE @i int = 11

WHILE @i <= 1000
BEGIN
	insert into TrainingTestQuestionNumbers(name,IsDeleted)values(cast(@i as nvarchar(max)),0)
    SET @i = @i + 1
    /* do some work */
END