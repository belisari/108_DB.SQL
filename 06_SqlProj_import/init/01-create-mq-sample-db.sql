-- Sample database + [mq] schema, standing in for the real work DB.
-- Just enough object variety (table, FK, view, stored proc, scalar function)
-- to exercise the SqlPackage extract -> sqlproj import workflow end to end.

IF DB_ID('MqSampleDb') IS NULL
BEGIN
    CREATE DATABASE MqSampleDb;
END
GO

USE MqSampleDb;
GO

IF SCHEMA_ID('mq') IS NULL
    EXEC('CREATE SCHEMA [mq]');
GO

IF OBJECT_ID('mq.QueueDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE mq.QueueDefinition
    (
        QueueDefinitionId INT IDENTITY(1,1) NOT NULL,
        Name              NVARCHAR(128)     NOT NULL,
        CreatedUtc        DATETIME2(3)      NOT NULL CONSTRAINT DF_QueueDefinition_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_QueueDefinition PRIMARY KEY CLUSTERED (QueueDefinitionId),
        CONSTRAINT UQ_QueueDefinition_Name UNIQUE (Name)
    );
END
GO

IF OBJECT_ID('mq.QueueMessage', 'U') IS NULL
BEGIN
    CREATE TABLE mq.QueueMessage
    (
        QueueMessageId    BIGINT IDENTITY(1,1) NOT NULL,
        QueueDefinitionId INT                   NOT NULL,
        Payload           NVARCHAR(MAX)         NOT NULL,
        EnqueuedUtc       DATETIME2(3)          NOT NULL CONSTRAINT DF_QueueMessage_EnqueuedUtc DEFAULT SYSUTCDATETIME(),
        ProcessedUtc      DATETIME2(3)          NULL,
        CONSTRAINT PK_QueueMessage PRIMARY KEY CLUSTERED (QueueMessageId),
        CONSTRAINT FK_QueueMessage_QueueDefinition FOREIGN KEY (QueueDefinitionId)
            REFERENCES mq.QueueDefinition (QueueDefinitionId)
    );
END
GO

IF OBJECT_ID('mq.vw_PendingMessages', 'V') IS NULL
    EXEC('
        CREATE VIEW mq.vw_PendingMessages AS
        SELECT m.QueueMessageId, m.QueueDefinitionId, q.Name AS QueueName, m.Payload, m.EnqueuedUtc
        FROM mq.QueueMessage m
        JOIN mq.QueueDefinition q ON q.QueueDefinitionId = m.QueueDefinitionId
        WHERE m.ProcessedUtc IS NULL
    ');
GO

IF OBJECT_ID('mq.fn_MessageCountForQueue', 'FN') IS NULL
    EXEC('
        CREATE FUNCTION mq.fn_MessageCountForQueue(@QueueDefinitionId INT)
        RETURNS INT
        AS
        BEGIN
            DECLARE @Count INT;
            SELECT @Count = COUNT(*) FROM mq.QueueMessage WHERE QueueDefinitionId = @QueueDefinitionId AND ProcessedUtc IS NULL;
            RETURN @Count;
        END
    ');
GO

IF OBJECT_ID('mq.usp_EnqueueMessage', 'P') IS NULL
    EXEC('
        CREATE PROCEDURE mq.usp_EnqueueMessage
            @QueueName NVARCHAR(128),
            @Payload   NVARCHAR(MAX)
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @QueueDefinitionId INT;

            SELECT @QueueDefinitionId = QueueDefinitionId FROM mq.QueueDefinition WHERE Name = @QueueName;
            IF @QueueDefinitionId IS NULL
            BEGIN
                INSERT INTO mq.QueueDefinition (Name) VALUES (@QueueName);
                SET @QueueDefinitionId = SCOPE_IDENTITY();
            END

            INSERT INTO mq.QueueMessage (QueueDefinitionId, Payload)
            VALUES (@QueueDefinitionId, @Payload);
        END
    ');
GO

-- A couple of sample rows so the extracted project has something realistic
-- behind the FK/unique constraints (schema-only extract ignores the data
-- itself, but it is handy for manually poking at the container).
IF NOT EXISTS (SELECT 1 FROM mq.QueueDefinition WHERE Name = 'orders.created')
BEGIN
    EXEC mq.usp_EnqueueMessage @QueueName = 'orders.created', @Payload = N'{"orderId":1}';
    EXEC mq.usp_EnqueueMessage @QueueName = 'orders.created', @Payload = N'{"orderId":2}';
END
GO
