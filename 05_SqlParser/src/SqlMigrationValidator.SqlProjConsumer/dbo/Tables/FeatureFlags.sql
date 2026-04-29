CREATE TABLE [dbo].[FeatureFlags]
(
    [FlagId]      INT             IDENTITY(1, 1) NOT NULL,
    [FlagName]    NVARCHAR(128)   NOT NULL,
    [IsEnabled]   BIT             NOT NULL CONSTRAINT [DF_FeatureFlags_IsEnabled] DEFAULT (0),
    [Description] NVARCHAR(512)   NULL,
    [CreatedAt]   DATETIME2(3)    NOT NULL CONSTRAINT [DF_FeatureFlags_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_FeatureFlags] PRIMARY KEY CLUSTERED ([FlagId] ASC),
    CONSTRAINT [UQ_FeatureFlags_FlagName] UNIQUE ([FlagName])
)
