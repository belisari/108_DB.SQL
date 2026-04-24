-- Backfill legacy order status codes.
-- NOTE: The EXEC (@sql) below intentionally triggers a DYNAMIC_SQL warning from the
-- SqlMigrationValidator task. Run `dotnet build` to see the warning in the build output.
-- In production scripts prefer static SQL; parameterise via sp_executesql if dynamic SQL
-- is truly unavoidable.

UPDATE dbo.Orders
SET    StatusCode = 'LEGACY_CLOSED'
WHERE  StatusCode = 'CLOSED'
  AND  CreatedDate < '2022-01-01'

-- Dynamic SQL example — will produce a build warning (not an error):
DECLARE @table NVARCHAR(128) = N'dbo.Orders'
DECLARE @sql   NVARCHAR(500) = N'SELECT COUNT(*) FROM ' + @table
EXEC (@sql)
