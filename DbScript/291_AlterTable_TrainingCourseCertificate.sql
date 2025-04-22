ALTER TABLE TrainingCourseCertificate ADD isRPLEnabled BIT DEFAULT 0

UPDATE TrainingCourseCertificate
SET isRPLEnabled = 1
WHERE Id IN (
    SELECT TrainingCourseCertificateId
    FROM TrainingCourseCertificateRPL
);
UPDATE TrainingCourseCertificate
SET isRPLEnabled = 0
WHERE Id NOT IN (
    SELECT TrainingCourseCertificateId
    FROM TrainingCourseCertificateRPL
);
ALTER TABLE TrainingCourseCertificateRPL ADD GuardId int
update TrainingCourseCertificateRPL set guardid=0


SELECT * FROM TrainingCourseCertificateRPL
SELECT * FROM TrainingCourseCertificate

