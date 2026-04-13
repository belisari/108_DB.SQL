-- Example: GOOD migration script
-- Only plain data manipulation. No DDL, no transaction control.

-- ✓ Idempotent guard using IF
IF NOT EXISTS (
    SELECT 1 FROM dbo.FeatureFlags WHERE FlagName = 'NewCheckoutFlow'
)
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagName, IsEnabled, CreatedAt)
    VALUES ('NewCheckoutFlow', 0, GETUTCDATE())
END

-- ✓ Conditional UPDATE
UPDATE dbo.Orders
SET    StatusCode = 'LEGACY_CLOSED'
WHERE  StatusCode = 'CLOSED'
  AND  CreatedDate < '2022-01-01'

-- ✓ DELETE with WHERE — targeted, not TRUNCATE
DELETE FROM dbo.AuditLog
WHERE  CreatedDate < DATEADD(YEAR, -7, GETUTCDATE())

-- ✓ INSERT from another table (data migration pattern)
INSERT INTO dbo.CustomerArchive (CustomerId, Email, ArchivedAt)
SELECT CustomerId, Email, GETUTCDATE()
FROM   dbo.Customers
WHERE  IsDeleted = 1
  AND  DeletedDate < DATEADD(YEAR, -2, GETUTCDATE())
