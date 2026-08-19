CREATE TABLE [mq].[QueueMessage] (
    [QueueMessageId]    BIGINT         IDENTITY (1, 1) NOT NULL,
    [QueueDefinitionId] INT            NOT NULL,
    [Payload]           NVARCHAR (MAX) NOT NULL,
    [EnqueuedUtc]       DATETIME2 (3)  CONSTRAINT [DF_QueueMessage_EnqueuedUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    [ProcessedUtc]      DATETIME2 (3)  NULL,
    CONSTRAINT [PK_QueueMessage] PRIMARY KEY CLUSTERED ([QueueMessageId] ASC),
    CONSTRAINT [FK_QueueMessage_QueueDefinition] FOREIGN KEY ([QueueDefinitionId]) REFERENCES [mq].[QueueDefinition] ([QueueDefinitionId])
);


GO

