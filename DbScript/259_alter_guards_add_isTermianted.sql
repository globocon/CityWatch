ALTER TABLE guards ADD IsTerminated BIT DEFAULT 0;
UPDATE guards SET IsTerminated = 0 WHERE IsTerminated IS NULL;