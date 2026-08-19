
        CREATE VIEW mq.vw_PendingMessages AS
        SELECT m.QueueMessageId, m.QueueDefinitionId, q.Name AS QueueName, m.Payload, m.EnqueuedUtc
        FROM mq.QueueMessage m
        JOIN mq.QueueDefinition q ON q.QueueDefinitionId = m.QueueDefinitionId
        WHERE m.ProcessedUtc IS NULL

GO

