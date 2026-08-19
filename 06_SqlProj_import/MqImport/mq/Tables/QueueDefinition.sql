CREATE TABLE [mq].[QueueDefinition] (
    [QueueDefinitionId] INT            IDENTITY (1, 1) NOT NULL,
    [Name]              NVARCHAR (128) NOT NULL,
    [CreatedUtc]        DATETIME2 (3)  CONSTRAINT [DF_QueueDefinition_CreatedUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    CONSTRAINT [PK_QueueDefinition] PRIMARY KEY CLUSTERED ([QueueDefinitionId] ASC),
    CONSTRAINT [UQ_QueueDefinition_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO

