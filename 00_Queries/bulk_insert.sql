select * from pictures;

declare @SQLstr nvarchar(1000)
SET @SQLstr = 'BULK INSERT pictures FROM c:\Source\100_DotNety\150_DB\test_bulk01.txt'
EXEC @SQLstr

sp_who;

insert into pictures (Id, Name) values ('1', 'name01');

SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME LIKE '%s%'