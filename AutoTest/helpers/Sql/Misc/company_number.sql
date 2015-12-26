/****** Script for SelectTopNRows command from SSMS  ******/
DECLARE @table TABLE (a varchar(128),b float)
insert into @table
SELECT DATASET_ID as a, count(DATASET_ID)as b
  FROM company.COMPANY_base b 
   where  TemplateCompanyID is null
   group by DATASET_ID
 
  select b as companies, count(b) as datasets, GETDATE() as Date
  INTO V_SQL_01.test.dbo.[company_count]
   from @table group by b
  order by b