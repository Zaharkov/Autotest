SELECT count (*) as Count, Type, getdate() as Date
INTO V_SQL_01.test.dbo.[OTVETSTVENNIE_LICA_result]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[OTVETSTVENNIE_LICA] 
  where Dt > '1900-01-01'
  group by [Type]
  
  SELECT count(OPF) as count,OPF, getdate() as Date
  INTO V_SQL_01.test.dbo.[company_opf]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[COMPANY_base] group by opf
  
  SELECT year(CREATE_DATE) as yearcreated, month(CREATE_DATE) as monthcreated, count(month(CREATE_DATE)) as companycount, getdate() as Date
  INTO V_SQL_01.test.dbo.[company_created]
  FROM [BO_M_VIRTUAL_COMPANIES].[company].[COMPANY_base] c 
     where  TemplateCompanyID is null
  group by month(CREATE_DATE),year(CREATE_DATE)  
  order by 1,2