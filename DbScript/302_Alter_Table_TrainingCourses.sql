alter table TrainingCourses add IsDeleted bit default 0
update TrainingCourses set IsDeleted=0
alter table TrainingCourseCertificate add IsDeleted bit default 0
update TrainingCourseCertificate  set IsDeleted=0