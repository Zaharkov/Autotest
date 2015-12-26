/****** Script for SelectTopNRows command from SSMS  ******/
DECLARE @table TABLE (a varchar(128),b float)
DECLARE @normalvalue float = 0.5
DECLARE @max int

insert into @table
SELECT [COMPANY_ID] as a, count(COMPANY_ID)as b
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[RASCHETNYJ_PERIOD] rr
  join company.COMPANY_base b on b.id = rr.COMPANY_ID
   where rr.Is_Closed=1 
   and TemplateCompanyID is null
   group by COMPANY_ID
 
 set @max = (select top 1 count(b) as companycount from @table group by b
  order by companycount desc) 
  
  select b as monthcount, count(b) as companycount, getdate() as Date INTO V_SQL_01.test.dbo.[company_rr_count] from @table group by b
  order by b
 
 --select b as monthcount,(1 - power(@normalvalue, case when count(b)> @max*.01 then count(b) else 0 end))/(1-@normalvalue) as companycount from @table group by b
 --  order by b    
  
 /* SELECT *
      
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[RASCHETNYJ_PERIOD] */