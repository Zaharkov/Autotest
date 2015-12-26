DECLARE @name NVARCHAR(200)
DECLARE @table TABLE (names varchar(128))
insert into @table(names)
values ('KOD_TARIFA'),('TARIF_VZNOSA_NS_I_PZ'),('SKIDKA_NADBAVKA_K_TARIFU_VZNOSA')
 
DECLARE vendor_cursor CURSOR for select * from @table

OPEN vendor_cursor

FETCH NEXT FROM vendor_cursor 
INTO @name

 WHILE @@FETCH_STATUS = 0
 BEGIN

exec ('select Value,count(*) as number, getdate() as Date
INTO V_SQL_01.test.dbo.['+@name+'_result]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].'+@name+' WITH (NOLOCK)
  group by Value order by 1')
   
  FETCH NEXT FROM vendor_cursor 
  INTO @name
  
 END
CLOSE vendor_cursor
DEALLOCATE vendor_cursor
