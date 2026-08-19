
        CREATE FUNCTION mq.fn_MessageCountForQueue(@QueueDefinitionId INT)
        RETURNS INT
        AS
        BEGIN
            DECLARE @Count INT;
            SELECT @Count = COUNT(*) FROM mq.QueueMessage WHERE QueueDefinitionId = @QueueDefinitionId AND ProcessedUtc IS NULL;
            RETURN @Count;
        END

GO

