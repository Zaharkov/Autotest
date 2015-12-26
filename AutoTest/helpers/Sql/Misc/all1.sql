--dropping base
DECLARE @name NVARCHAR(200)
DECLARE @table TABLE (names varchar(128))
--insert into @table
 SELECT t.name FROM sys.schemas s
 JOIN sys.tables t ON t.schema_id = s.schema_id
 OUTER APPLY (SELECT COUNT(1) cn FROM sys.columns WHERE object_id = t.object_id) c 
 WHERE s.name = 'dbo'
 
DECLARE vendor_cursor CURSOR for select * from @table

OPEN vendor_cursor
FETCH NEXT FROM vendor_cursor 
INTO @name
 WHILE @@FETCH_STATUS = 0
 BEGIN 
exec ('drop table ' + @name)   
  FETCH NEXT FROM vendor_cursor 
  INTO @name  
 END
CLOSE vendor_cursor
DEALLOCATE vendor_cursor

go
--various
SELECT count (*) as Count, Type, getdate() as Date
INTO test.dbo.[OTVETSTVENNIE_LICA_result]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[OTVETSTVENNIE_LICA] 
  where Dt > '1900-01-01'
  group by [Type]
  
  SELECT count(OPF) as count,OPF, getdate() as Date
  INTO test.dbo.[company_opf]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[COMPANY_base] group by opf
  
  SELECT year(CREATE_DATE) as yearcreated, month(CREATE_DATE) as monthcreated, count(month(CREATE_DATE)) as companycount, getdate() as Date
  INTO test.dbo.[company_created]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[COMPANY_base] c 
     where  TemplateCompanyID is null
  group by month(CREATE_DATE),year(CREATE_DATE)  
  order by 1,2  
GO

--values
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
INTO test.dbo.['+@name+'_values_result]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].'+@name+' WITH (NOLOCK)
  group by Value order by 1')
   
  FETCH NEXT FROM vendor_cursor 
  INTO @name
  
 END
CLOSE vendor_cursor
DEALLOCATE vendor_cursor;
GO
 
 --payment_dates
 DECLARE @table TABLE (a varchar(128),b float)

insert into @table
SELECT [COMPANY_ID] as a, count(COMPANY_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[PaymentDates] av
  join [BO_M_VIRTUAL_COMPANIES].company.RASCHETNYJ_PERIOD rr on rr.ID = av.CompanyPeriodIdStart 
  group by COMPANY_ID
 
 select b as monthcount, count(b) as companycount, GETDATE() as Date
  INTO test.dbo.[company_payment_days_count]
   from @table group by b  
  order by b
  
declare @days table (day int, avanscount int, wagecount int)
insert into @days
SELECT AvansDay, count(AvansDay), 0 
FROM [BO_M_VIRTUAL_COMPANIES].[company].[PaymentDates]  with (NOLOCK) group by AvansDay

MERGE INTO @days A
   USING (SELECT WageDay as day,  0 as avanscount,  count(WageDay) as wagecount FROM [BO_M_VIRTUAL_COMPANIES].[company].[PaymentDates] with (NOLOCK) group by WageDay) as V 
      ON A.day = V.day         
WHEN MATCHED THEN
   UPDATE 
      SET wagecount = V.wagecount
WHEN NOT MATCHED THEN
    INSERT (day, avanscount, wagecount)
    VALUES (V.day,V.avanscount,V.wagecount);    

select *,GETDATE() as Date
INTO test.dbo.[company_payment_days] 
from @days order by 1
GO

--employeedt
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

select *,getdate() as Date 
INTO test.dbo.[employeedt_result]
from @result order by 2 desc
GO

--companyDt
DECLARE @name NVARCHAR(200)
DECLARE @table TABLE (names varchar(128))
declare @result table (names varchar(128), rowcoun int);
insert into @table
 SELECT t.name FROM sys.schemas s
 JOIN sys.tables t ON t.schema_id = s.schema_id
 OUTER APPLY (SELECT COUNT(1) cn FROM sys.columns WHERE object_id = t.object_id and (name='dt' OR name ='Dt')) c 
 WHERE s.name = 'company' and c.cn >=1
 
DECLARE vendor_cursor CURSOR for select * from @table

OPEN vendor_cursor

FETCH NEXT FROM vendor_cursor 
INTO @name

 WHILE @@FETCH_STATUS = 0
 BEGIN
 
insert into @result (rowcoun, names)
exec ('select count(*),'''+@name+'''
  FROM [BO_M_VIRTUAL_COMPANIES].[company].'+@name+' WITH (NOLOCK)
  where dt > ''1900-01-01''')
   
  FETCH NEXT FROM vendor_cursor 
  INTO @name
  
 END
CLOSE vendor_cursor
DEALLOCATE vendor_cursor

select *,getdate() as Date INTO test.dbo.[companydt_result]
from @result order by 2 desc
GO

--company_number
DECLARE @table TABLE (a varchar(128),b float)
insert into @table
SELECT DATASET_ID as a, count(DATASET_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].company.COMPANY_base b 
   where  TemplateCompanyID is null
   group by DATASET_ID
 
  select b as companies, count(b) as datasets, GETDATE() as Date
  INTO test.dbo.[company_count]
   from @table group by b
  order by b
  GO
  
  --avans
  DECLARE @table TABLE (a varchar(128),b float)
insert into @table
SELECT [COMPANY_ID] as a, count(COMPANY_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[Avans] av
  join [BO_M_VIRTUAL_COMPANIES].company.RASCHETNYJ_PERIOD rr on rr.ID = av.CompanyPeriodId  
  group by COMPANY_ID
 
  select b as avanschanges, count(b) as companycount, GETDATE() as Date
  INTO test.dbo.[company_avans_count]
 from @table group by b
  order by b
  
SELECT Summa, count (Summa) as count, GETDATE() as Date
INTO test.dbo.[avans_values_count]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[Avans] group by Summa
  order by Summa
  GO