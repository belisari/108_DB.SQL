
use [test01];
select * from [dbo].Users;

select Name from Users where Id = 4;


---- strings
-- + dzia³a na char(10), nie na text
SELECT 'book'+'case';
select Name + ' jjhhyy' from Users where Id = 1; --char(50)
select Description + ' jjhhyy' from [dbo].Users where Id = 1;   --varchar(200)


--
SELECT CURRENT_USER;  --dbo   !!! bo jest tylko taki user; tu nie ma emilw, bo emilw to jest Login
SELECT SYSTEM_USER;  --dbo   !!! bo jest tylko taki user; tu nie ma emilw, bo emilw to jest Login

SELECT QUOTENAME('EMIL')

DECLARE @nameTable NVARCHAR(255);
SET @nameTable = '[dbo].WOTA'
SET @nameTable = (SELECT REPLACE(@nameTable,'[',''))
SET @nameTable = (SELECT REPLACE(@nameTable,']',''))
SET @nameTable = QUOTENAME(@nameTable)  --dodaje [] wokó³ tekstu
SET @nameTable = (SELECT REPLACE(@nameTable,'.','].['))
SELECT @nameTable  --[dbo].[WOTA]


SET @QUERY = N'TRUNCATE TABLE ' + @nameTable + ';'




--mogê siê zalogowaæ jako tt_sql_replatform i sprawdziæ;

SELECT IS_MEMBER('db_owner');


select * from Users;





----------------------------
--https://dba.stackexchange.com/questions/160550/why-does-session-user-return-dbo-instead-of-sql-login#:~:text=Each%20Database%20has%20a%20single,is%20the%20DB%20in%20question.&text=SESSION_USER%20can%20return%20a%20Login,the%20DB%20to%20map%20to.
USE [master];
CREATE LOGIN [GazooLogin] WITH PASSWORD = 'NevrCrack';
CREATE DATABASE [GazooDB] COLLATE Latin1_General_100_CI_AS;
ALTER AUTHORIZATION ON DATABASE::[GazooDB] TO [sa];
GO

CREATE USER [GazooUser1]
  FROM LOGIN [GazooLogin]
  WITH DEFAULT_SCHEMA = [GazooSchema1];
GO

CREATE SCHEMA [GazooSchema1]
  AUTHORIZATION [GazooUser1];
GO


USE [GazooDB];
CREATE USER [GazooUser2]
  FROM LOGIN [GazooLogin]
  WITH DEFAULT_SCHEMA = [GazooSchema2];
GO

CREATE SCHEMA [GazooSchema2]
  AUTHORIZATION [GazooUser2];
GO

---------------

SELECT * FROM sys.fn_my_permissions(NULL, N'database')
-- database     CONNECT

SELECT SESSION_USER AS [SESSION_USER],
       ORIGINAL_LOGIN() AS [ORIGINAL_LOGIN],
       SUSER_SNAME() AS [SUSER_SNAME],
       SUSER_NAME() AS [SUSER_NAME];
-- GazooUser2   Dali\Solomon    GazooLogin  GazooLogin

USE [master];
SELECT * FROM sys.fn_my_permissions(NULL, N'database')
-- database     CONNECT

SELECT SESSION_USER AS [SESSION_USER],
       ORIGINAL_LOGIN() AS [ORIGINAL_LOGIN],
       SUSER_SNAME() AS [SUSER_SNAME],
       SUSER_NAME() AS [SUSER_NAME];
-- GazooUser1   Dali\Solomon    GazooLogin  GazooLogin


USE [GazooDB]; -- can only revert from DB where EXECUTE AS was run
REVERT;



