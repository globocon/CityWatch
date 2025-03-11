insert into TrainingCourseStatus(ReferenceNo,Name) values('04','Completed')
update GuardTrainingAndAssessment  set TrainingCourseStatusId=4 where IsCompleted=1

SELECT OBJECT_NAME(dc.object_id) AS ConstraintName
FROM sys.default_constraints dc
JOIN sys.columns c ON c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('GuardTrainingAndAssessment')
  AND c.name = 'IsCompleted';

  ALTER TABLE GuardTrainingAndAssessment
DROP CONSTRAINT DF__GuardTrai__IsCom__46DD686B;

alter table GuardTrainingAndAssessment drop column IsCompleted
