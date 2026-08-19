
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

GO

