CREATE TABLE [dbo].[Orders]
(
    [OrderId]     INT             IDENTITY(1, 1) NOT NULL,
    [CustomerId]  INT             NOT NULL,
    [StatusCode]  NVARCHAR(32)    NOT NULL,
    [TotalAmount] DECIMAL(18, 2)  NOT NULL,
    [CreatedDate] DATETIME2(3)    NOT NULL CONSTRAINT [DF_Orders_CreatedDate] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderId] ASC)
)
