alter table GuardTrainingAndAssessment drop column description
ALTER TABLE TrainingTestFeedbackQuestions DROP CONSTRAINT FK__TrainingT__HRSet__03275C9C
alter table TrainingTestFeedbackQuestions drop column hrsettingsId

DECLARE @counter INT = 3;

WHILE @counter <= 12
BEGIN
    update TrainingTestFeedbackQuestions set IsDeleted=1 where Id=@counter
	set @counter = @counter + 1
END
