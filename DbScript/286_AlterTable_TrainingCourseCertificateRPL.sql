
alter table TrainingCourseCertificateRPL add  isDeleted bit default 0
update TrainingCourseCertificateRPL set isDeleted=0