select * from pictures;


declare @SQLstr nvarchar(1000)
SET @SQLstr = 'BULK INSERT pictures FROM "c:\Source\100_DotNety\108_DB.SQL\01_BulkInsert\b01.txt"' + 
'WITH (FIELDTERMINATOR = '','', CODEPAGE=''RAW'' )'
print @SQLstr
EXEC sp_executesql @SQLstr

sp_who;



insert into pictures (Id, Name) values ('1', 'name01');

SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME LIKE '%s%'