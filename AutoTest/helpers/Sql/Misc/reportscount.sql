/****** Script for SelectTopNRows command from SSMS  ******/
SELECT count(*) as reportcount, month(DateCreate) as [month], year(DateCreate) as [year]
  FROM [BO_M_VIRTUAL_COMPANIES].[dbo].[CommissionedReports] cr
  join [BO_M_VIRTUAL_COMPANIES].company.COMPANY_base cb on cb.ID = cr.CompanyId
  where cb.TemplateCompanyID is null
  group by month(DateCreate), year(DateCreate) 
  order by 2
  
SELECT count(*) as reportcount,trd.name, [year]
FROM [BO_M_VIRTUAL_COMPANIES].[dbo].[CommissionedReports] cr
join (select temp.id, 'Kvartal'+ cast(temp.KvartalNumber as varchar)+' ' + temp.TypeCode as name from [BO_M_VIRTUAL_COMPANIES].dbo.TermsReportDate temp) trd on trd.Id=cr.TermsReportDateId
join [BO_M_VIRTUAL_COMPANIES].company.COMPANY_base cb on cb.ID = cr.CompanyId
where cb.TemplateCompanyID is null
group by trd.name, [year]
order by 3,2 