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

PRINT @@ERROR;

PRINT @@NON_EXISTING_GLOBAL_VARIABLE;


-- Ok
EXEC dbo.CleanupOrders

-- Ok
DECLARE @cutoffDate DATETIME = '2020-01-01'
EXEC dbo.CleanupOrders @cutoffDate = '2020-01-01'
EXECUTE dbo.SomeProc @cutoffDate;

EXECUTE dbo.SomeProc @NOT_DECLARED_VAR;

-- WARNING — dynamic string — EXEC (@variable)
DECLARE @sql NVARCHAR(500) = 'UPDATE dbo.Orders SET Status = 1';
EXEC (@sql);

-- WARNING — dynamic string — EXEC with a string literal directly
EXEC ('UPDATE dbo.Orders SET Status = 1');

-- sp_executesql
DECLARE @sql NVARCHAR(500) = N'UPDATE dbo.Orders SET Status = 1'
EXEC sp_executesql @sql;
