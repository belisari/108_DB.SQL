-- Example: BAD migration script
-- This file demonstrates everything the validator will REJECT.

-- Switching database context
USE [MyDatabase]
GO

-- Opening a nested transaction
BEGIN TRANSACTION

    -- This is fine — plain data changes are allowed
    UPDATE dbo.Customers
    SET    IsActive = 1
    WHERE  CreatedDate < '2020-01-01'

    -- Schema change — belongs in dacpac
    ALTER TABLE dbo.Customers ADD LegacyFlag BIT NOT NULL DEFAULT 0

    -- Index DDL — belongs in dacpac
    CREATE INDEX IX_Customers_LegacyFlag ON dbo.Customers (LegacyFlag)

    -- Reading transaction nesting state
    IF @@TRANCOUNT > 0
       PRINT 'still inside a transaction'


    -- Explicit commit inside the outer transaction
    COMMIT TRANSACTION

-- Truncate instead of DELETE
TRUNCATE TABLE dbo.StagingImport

-- Legacy error-handling global (use TRY/CATCH instead)
UPDATE dbo.Orders SET StatusCode = 'DONE' WHERE Id = 1
    IF @@ERROR <> 0
    ROLLBACK

-- Session identity global — unreliable in combined scripts
INSERT INTO dbo.Audit (EntityId, CreatedAt) VALUES (@@IDENTITY, GETUTCDATE())
