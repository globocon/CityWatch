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

SELECT * FROM TrainingCourseCertificateRPL
SELECT * FROM TrainingCourseCertificate

