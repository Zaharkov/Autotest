declare @table TABLE (
EMPLOYEE_ID uniqueidentifier, 
COMPANY_ID uniqueidentifier, 
vaca_number int, 
sick_number int,
bustr_number int,
nev_number int,
ADRES_REG                                                int,
AVANS                                                    int,
CHARGE                                                   int,
DOHODY_S_PRED_MESTA_RABOTY                               int,
DOKUMENT                                                 int,
EARLY_DOHODY_GPD                                         int,
EARLY_DOHODY_NDFL                                        int,
EARLY_DOHODY_STRAH                                       int,
EARLY_OTKLONENIYA                                        int,
EARLY_OTPUSK_DAY                                         int,
EARLY_TARIF                                              int,
FAMILY_MEMBER                                            int,
FIO                                                      int,
GRAJDANSTVO                                              int,
INN_FIZ                                                  int,
INVALIDNOST                                              int,
KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV      int,
LABOR_AWARD                                              int,
POL_NEW                                                  int,
RAJONNY_FEDERALNY_KOEFFICIENT                            int,
RAJONNY_MESTNY_KOEFFICIENT                               int,
REZIDENTSKY_STATUS                                       int,
SEVERNAJA_NADBAVKA                                       int,
SNILS                                                    int,
SOTRUDNIK                                                int,
SOTRUDNIK_RABOTAL_NDFL                                   int,
SPRAVKA_STRAHOVYH_DETAILS                                int,
STRAHOVOY_STAJ                                           int,
TERR_USLOVIJA_PFR                                        int,
VYCHET_PO_NDFL_DETSKIJ                                   int,
VYCHET_PO_NDFL_LICHNYJ                                   int,
CONTRACT_LABOR_CHANGE                                    int,
CONTRACT_CIVIL int)

insert into @table
select con.EMPLOYEE_ID, con.COMPANY_ID, vaca.cn as vaca_number, sick.cn as sick_number, bustr.cn as bustr_number, nev.cn as nev_number,
ADRES_REG                                          .cn as ADRES_REG                                          , 
AVANS                                              .cn as AVANS                                              , 
CHARGE                                             .cn as CHARGE                                             , 
DOHODY_S_PRED_MESTA_RABOTY                         .cn as DOHODY_S_PRED_MESTA_RABOTY                         , 
DOKUMENT                                           .cn as DOKUMENT                                           , 
EARLY_DOHODY_GPD                                   .cn as EARLY_DOHODY_GPD                                   , 
EARLY_DOHODY_NDFL                                  .cn as EARLY_DOHODY_NDFL                                  , 
EARLY_DOHODY_STRAH                                 .cn as EARLY_DOHODY_STRAH                                 , 
EARLY_OTKLONENIYA                                  .cn as EARLY_OTKLONENIYA                                  , 
EARLY_OTPUSK_DAY                                   .cn as EARLY_OTPUSK_DAY                                   , 
EARLY_TARIF                                        .cn as EARLY_TARIF                                        , 
FAMILY_MEMBER                                      .cn as FAMILY_MEMBER                                      , 
FIO                                                .cn as FIO                                                , 
GRAJDANSTVO                                        .cn as GRAJDANSTVO                                        , 
INN_FIZ                                            .cn as INN_FIZ                                            , 
INVALIDNOST                                        .cn as INVALIDNOST                                        , 
KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV.cn as KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV, 
LABOR_AWARD                                        .cn as LABOR_AWARD                                        , 
POL_NEW                                            .cn as POL_NEW                                            , 
RAJONNY_FEDERALNY_KOEFFICIENT                      .cn as RAJONNY_FEDERALNY_KOEFFICIENT                      , 
RAJONNY_MESTNY_KOEFFICIENT                         .cn as RAJONNY_MESTNY_KOEFFICIENT                         , 
REZIDENTSKY_STATUS                                 .cn as REZIDENTSKY_STATUS                                 , 
SEVERNAJA_NADBAVKA                                 .cn as SEVERNAJA_NADBAVKA                                 , 
SNILS                                              .cn as SNILS                                              , 
SOTRUDNIK                                          .cn as SOTRUDNIK                                          , 
SOTRUDNIK_RABOTAL_NDFL                             .cn as SOTRUDNIK_RABOTAL_NDFL                             , 
SPRAVKA_STRAHOVYH_DETAILS                          .cn as SPRAVKA_STRAHOVYH_DETAILS                          , 
STRAHOVOY_STAJ                                     .cn as STRAHOVOY_STAJ                                     , 
TERR_USLOVIJA_PFR                                  .cn as TERR_USLOVIJA_PFR                                  , 
VYCHET_PO_NDFL_DETSKIJ                             .cn as VYCHET_PO_NDFL_DETSKIJ                             , 
VYCHET_PO_NDFL_LICHNYJ                             .cn as VYCHET_PO_NDFL_LICHNYJ                             , 
CONTRACT_LABOR_CHANGE                              .cn as CONTRACT_LABOR_CHANGE,
CONTRACT_CIVIL.cn as CONTRACT_CIVIL
	from [BO_M_VIRTUAL_COMPANIES].hire_contract.CONTRACT con with (nolock) 
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee_sicklist.SICKLIST sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) sick
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee_otpusk.OTPUSK sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) vaca
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee_komandirovka.KOMANDIROVKA sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) bustr 
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee_nevyhod.NEVYHOD sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) nev
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.ADRES_PROJ sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) ADRES_PROJ
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.ADRES_REG sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) ADRES_REG
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.AVANS sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) AVANS
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.CHARGE sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) CHARGE
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.DOHODY_S_PRED_MESTA_RABOTY sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) DOHODY_S_PRED_MESTA_RABOTY
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.DOKUMENT sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) DOKUMENT
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_DOHODY_GPD sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_DOHODY_GPD
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_DOHODY_NDFL sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_DOHODY_NDFL
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_DOHODY_STRAH sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_DOHODY_STRAH
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_OTKLONENIYA sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_OTKLONENIYA
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_OTPUSK_DAY sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_OTPUSK_DAY
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.EARLY_TARIF sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) EARLY_TARIF
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.FAMILY_MEMBER sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) FAMILY_MEMBER
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.FIO sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) FIO
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.GRAJDANSTVO sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) GRAJDANSTVO
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.INN_FIZ sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) INN_FIZ
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.INVALIDNOST sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) INVALIDNOST
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.LABOR_AWARD sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) LABOR_AWARD
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.POL_NEW sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) POL_NEW
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.RAJONNY_FEDERALNY_KOEFFICIENT sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) RAJONNY_FEDERALNY_KOEFFICIENT
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.RAJONNY_MESTNY_KOEFFICIENT sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) RAJONNY_MESTNY_KOEFFICIENT
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.REZIDENTSKY_STATUS sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) REZIDENTSKY_STATUS
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.SEVERNAJA_NADBAVKA sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) SEVERNAJA_NADBAVKA
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.SNILS sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) SNILS
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.SOTRUDNIK sl with (nolock) where sl.Id = con.EMPLOYEE_ID) SOTRUDNIK
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.SOTRUDNIK_RABOTAL_NDFL sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) SOTRUDNIK_RABOTAL_NDFL
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.SPRAVKA_STRAHOVYH_DETAILS sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) SPRAVKA_STRAHOVYH_DETAILS
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.STRAHOVOY_STAJ sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) STRAHOVOY_STAJ
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.TERR_USLOVIJA_PFR sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) TERR_USLOVIJA_PFR
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.VYCHET_PO_NDFL_DETSKIJ sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) VYCHET_PO_NDFL_DETSKIJ
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].employee.VYCHET_PO_NDFL_LICHNYJ sl with (nolock) where sl.EMPLOYEE_ID = con.EMPLOYEE_ID) VYCHET_PO_NDFL_LICHNYJ
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].hire_contract.CONTRACT_LABOR_CHANGE sl with (nolock) where sl.EmployeeId = con.EMPLOYEE_ID) CONTRACT_LABOR_CHANGE
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].hire_contract.CONTRACT_CIVIL sl with (nolock) where sl.ID = con.id) CONTRACT_CIVIL
		
	--drop table test.dbo.employee_data

SELECT [ID] as company_id,[TranslitName], en.cn as employee_number, 
	vaca_number,
	sick_number,
	nev_number,
	bustr_number, 
	ADRES_REG,                                           
	AVANS                                              , 
	CHARGE                                             , 
	DOHODY_S_PRED_MESTA_RABOTY                         , 
	DOKUMENT                                           , 
	EARLY_DOHODY_GPD                                   , 
	EARLY_DOHODY_NDFL                                  , 
	EARLY_DOHODY_STRAH                                 , 
	EARLY_OTKLONENIYA                                  , 
	EARLY_OTPUSK_DAY                                   , 
	EARLY_TARIF                                        , 
	FAMILY_MEMBER                                      , 
	FIO                                                , 
	GRAJDANSTVO                                        , 
	INN_FIZ                                            , 
	INVALIDNOST                                        , 
	KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV, 
	LABOR_AWARD                                        , 
	POL_NEW                                            , 
	RAJONNY_FEDERALNY_KOEFFICIENT                      , 
	RAJONNY_MESTNY_KOEFFICIENT                         , 
	REZIDENTSKY_STATUS                                 , 
	SEVERNAJA_NADBAVKA                                 , 
	SNILS                                              , 
	SOTRUDNIK                                          , 
	SOTRUDNIK_RABOTAL_NDFL                             , 
	SPRAVKA_STRAHOVYH_DETAILS                          , 
	STRAHOVOY_STAJ                                     , 
	TERR_USLOVIJA_PFR                                  , 
	VYCHET_PO_NDFL_DETSKIJ                             , 
	VYCHET_PO_NDFL_LICHNYJ                             , 
	CONTRACT_LABOR_CHANGE  , 
	CONTRACT_CIVIL,
	[RASCHETNYJ_PERIOD].cn as [RASCHETNYJ_PERIOD]
--INTO dbo.[employee_data]
FROM [BO_M_VIRTUAL_COMPANIES].[company].[COMPANY_base] c with (nolock) 
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].hire_contract.CONTRACT con with (nolock) where Company_Id= c.id) en 
	CROSS APPLY (SELECT count(*) cn from [BO_M_VIRTUAL_COMPANIES].[company].[RASCHETNYJ_PERIOD] sl with (nolock) where sl.COMPANY_ID = c.ID and sl.IS_CLOSED=1 ) [RASCHETNYJ_PERIOD]
	
	CROSS APPLY (SELECT 
	sum(b.vaca_number) as vaca_number, 
	sum(b.sick_number) as sick_number, 
	sum(b.nev_number) as nev_number, 
	sum(b.bustr_number) as bustr_number,
	sum(b.ADRES_REG                                          ) as  ADRES_REG                                          ,
	sum(b.AVANS                                              ) as  AVANS                                              ,
	sum(b.CHARGE                                             ) as  CHARGE                                             ,
	sum(b.DOHODY_S_PRED_MESTA_RABOTY                         ) as  DOHODY_S_PRED_MESTA_RABOTY                         ,
	sum(b.DOKUMENT                                           ) as  DOKUMENT                                           ,
	sum(b.EARLY_DOHODY_GPD                                   ) as  EARLY_DOHODY_GPD                                   ,
	sum(b.EARLY_DOHODY_NDFL                                  ) as  EARLY_DOHODY_NDFL                                  ,
	sum(b.EARLY_DOHODY_STRAH                                 ) as  EARLY_DOHODY_STRAH                                 ,
	sum(b.EARLY_OTKLONENIYA                                  ) as  EARLY_OTKLONENIYA                                  ,
	sum(b.EARLY_OTPUSK_DAY                                   ) as  EARLY_OTPUSK_DAY                                   ,
	sum(b.EARLY_TARIF                                        ) as  EARLY_TARIF                                        ,
	sum(b.FAMILY_MEMBER                                      ) as  FAMILY_MEMBER                                      ,
	sum(b.FIO                                                ) as  FIO                                                ,
	sum(b.GRAJDANSTVO                                        ) as  GRAJDANSTVO                                        ,
	sum(b.INN_FIZ                                            ) as  INN_FIZ                                            ,
	sum(b.INVALIDNOST                                        ) as  INVALIDNOST                                        ,
	sum(b.KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV) as  KATEGORIA_SOTRUDNIKA_DLA_RASCHETA_STRAHOVYH_VZNOSOV,
	sum(b.LABOR_AWARD                                        ) as  LABOR_AWARD                                        ,
	sum(b.POL_NEW                                            ) as  POL_NEW                                            ,
	sum(b.RAJONNY_FEDERALNY_KOEFFICIENT                      ) as  RAJONNY_FEDERALNY_KOEFFICIENT                      ,
	sum(b.RAJONNY_MESTNY_KOEFFICIENT                         ) as  RAJONNY_MESTNY_KOEFFICIENT                         ,
	sum(b.REZIDENTSKY_STATUS                                 ) as  REZIDENTSKY_STATUS                                 ,
	sum(b.SEVERNAJA_NADBAVKA                                 ) as  SEVERNAJA_NADBAVKA                                 ,
	sum(b.SNILS                                              ) as  SNILS                                              ,
	sum(b.SOTRUDNIK                                          ) as  SOTRUDNIK                                          ,
	sum(b.SOTRUDNIK_RABOTAL_NDFL                             ) as  SOTRUDNIK_RABOTAL_NDFL                             ,
	sum(b.SPRAVKA_STRAHOVYH_DETAILS                          ) as  SPRAVKA_STRAHOVYH_DETAILS                          ,
	sum(b.STRAHOVOY_STAJ                                     ) as  STRAHOVOY_STAJ                                     ,
	sum(b.TERR_USLOVIJA_PFR                                  ) as  TERR_USLOVIJA_PFR                                  ,
	sum(b.VYCHET_PO_NDFL_DETSKIJ                             ) as  VYCHET_PO_NDFL_DETSKIJ                             ,
	sum(b.VYCHET_PO_NDFL_LICHNYJ                             ) as  VYCHET_PO_NDFL_LICHNYJ                             ,
	sum(b.CONTRACT_LABOR_CHANGE                              ) as  CONTRACT_LABOR_CHANGE,
	sum(b.CONTRACT_CIVIL                              ) as  CONTRACT_CIVIL
	
		from @table b 
	 where b.COMPANY_ID = c.ID) vaca 
 where 
 TemplateCompanyID is NULL and en.cn >0 and IsDeleted = 0 and c.CREATE_DATE > '2014-01-01'
 
 --select * from test.dbo.employee_data