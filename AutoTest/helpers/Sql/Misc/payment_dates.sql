DECLARE @table TABLE (a varchar(128),b float)

insert into @table
SELECT [COMPANY_ID] as a, count(COMPANY_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[PaymentDates] av
  join company.RASCHETNYJ_PERIOD rr on rr.ID = av.CompanyPeriodIdStart 
  group by COMPANY_ID
 
 select b as monthcount, count(b) as companycount, GETDATE() as Date
  INTO V_SQL_01.test.dbo.[company_payment_days_count]
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
INTO V_SQL_01.test.dbo.[company_payment_days] 
from @days order by 1