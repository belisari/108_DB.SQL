-- Seed initial feature flags. Clean migration — no DDL, no transaction control.

IF NOT EXISTS (
    SELECT 1 FROM dbo.FeatureFlags WHERE FlagName = 'NewCheckoutFlow'
)
BEGIN
    INSERT INTO dbo.FeatureFlags (FlagName, IsEnabled, CreatedAt)
    VALUES ('NewCheckoutFlow', 0, GETUTCDATE())
END

UPDATE dbo.FeatureFlags
SET    Description = 'Enables the redesigned checkout experience'
WHERE  FlagName = 'NewCheckoutFlow'
  AND  Description IS NULL
