DECLARE @table TABLE (a varchar(128),b float)
insert into @table
SELECT [COMPANY_ID] as a, count(COMPANY_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[Avans] av
  join company.RASCHETNYJ_PERIOD rr on rr.ID = av.CompanyPeriodId  
  group by COMPANY_ID
 
  select b as avanschanges, count(b) as companycount, GETDATE() as Date
  INTO V_SQL_01.test.dbo.[company_avans_count]
 from @table group by b
  order by b
  
SELECT Summa, count (Summa) as count, GETDATE() as Date
INTO V_SQL_01.test.dbo.[avans_count]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[Avans] group by Summa
  order by Summa