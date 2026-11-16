-- Audit Users.Email duplicates before applying the AddUniqueUserEmailIndex migration.
-- The unique index creation fails if any of these rows exist, so resolve them first.
SELECT
    LOWER(LTRIM(RTRIM([Email]))) AS NormalizedEmail,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST([Id] AS nvarchar(36)), ', ') AS UserIds
FROM [Users]
GROUP BY LOWER(LTRIM(RTRIM([Email])))
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- Inspect suspicious rows (same email after trimming/casing):
-- SELECT [Id], [Email], [Name], [Role], [CreatedAt]
-- FROM [Users]
-- WHERE LOWER(LTRIM(RTRIM([Email]))) IN (
--     SELECT LOWER(LTRIM(RTRIM([Email]))) FROM [Users]
--     GROUP BY LOWER(LTRIM(RTRIM([Email]))) HAVING COUNT(*) > 1
-- )
-- ORDER BY [Email], [CreatedAt];
