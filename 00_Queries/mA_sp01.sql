USE [TREV_CERT]
GO
/****** Object:  StoredProcedure [dbo].[uspTruncate]    Script Date: 2020-08-28 13:49:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[uspTruncate]
       @nameTable varchar(60)
AS

SET NOCOUNT OFF;

/*
IF IS_MEMBER('mAltaBATCH') = 0
BEGIN
       THROW 60000, 'Must be member of mAltaBATCH role to execute [dbo].[uspTruncate].', 1
END
*/

DECLARE @QUERY NVARCHAR(255);

-- remove quotes
SET @nameTable = (SELECT REPLACE(@nameTable,'[',''))
SET @nameTable = (SELECT REPLACE(@nameTable,']',''))
SET @nameTable = QUOTENAME(@nameTable)
SET @nameTable = (SELECT REPLACE(@nameTable,'.','].['))

SET @QUERY = N'TRUNCATE TABLE ' + @nameTable + ';'

EXECUTE sp_executesql @QUERY;


DECLARE @nameTable NVARCHAR(255);
SET @nameTable = '[dbo].WOTA'
SET @nameTable = (SELECT REPLACE(@nameTable,'[',''))
SET @nameTable = (SELECT REPLACE(@nameTable,']',''))
SET @nameTable = QUOTENAME(@nameTable)  --dodaje [] wokół tekstu
SET @nameTable = (SELECT REPLACE(@nameTable,'.','].['))
SELECT @nameTable  --[dbo].[WOTA]