


alter table GuardTrainingAndAssessment add TrainingCourseStatusId int 
alter table GuardTrainingAndAssessment drop column HRGroup 
alter table GuardTrainingAndAssessment add HRGroupId int 
alter table GuardTrainingAndAssessment drop column CourseId 
alter table GuardTrainingAndAssessment add TrainingCourseId int 
 

ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([GuardId])
REFERENCES [dbo].[Guards] ([Id])
ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([HRGroupId])
REFERENCES [dbo].[HRGroups] ([Id])
ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([TrainingCourseStatusId])
REFERENCES [dbo].[TrainingCourseStatus] ([Id])
ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([TrainingCourseId])
REFERENCES [dbo].[TrainingCourses] ([Id])