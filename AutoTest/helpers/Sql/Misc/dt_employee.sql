DECLARE @name NVARCHAR(200)
DECLARE @table TABLE (names varchar(128))
declare @result table (names varchar(128), rowcoun int);
insert into @table
 SELECT t.name FROM sys.schemas s
 JOIN sys.tables t ON t.schema_id = s.schema_id
 OUTER APPLY (SELECT COUNT(1) cn FROM sys.columns WHERE object_id = t.object_id and (name='dt' OR name ='Dt')) c 
 WHERE s.name = 'employee' and c.cn >=1
 
DECLARE vendor_cursor CURSOR for select * from @table

OPEN vendor_cursor

FETCH NEXT FROM vendor_cursor 
INTO @name

 WHILE @@FETCH_STATUS = 0
 BEGIN
 
insert into @result (rowcoun, names)
exec ('select count(*),'''+@name+'''
  FROM [BO_M_VIRTUAL_COMPANIES].[employee].'+@name+' WITH (NOLOCK)
  where dt > ''1900-01-01''')
   
  FETCH NEXT FROM vendor_cursor 
  INTO @name
  
 END
CLOSE vendor_cursor
DEALLOCATE vendor_cursor

select *,getdate() as Date INTO V_SQL_01.test.dbo.[companydt_result]
from @result order by 2 desc