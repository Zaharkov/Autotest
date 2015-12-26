
GO
/****** Object:  StoredProcedure [dbo].[GetErrorsInfo2]    Script Date: 12/26/2015 19:09:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[GetErrorsInfo2](@parallelId uniqueidentifier) as 

BEGIN
SELECT [Id]
      ,[TestId]
      ,[ParallelId]
      ,[Text]
      ,[Type]
      ,[Line]
      ,[Time]
      ,[Bug]
      ,[ScreenPath]
      ,[Checked]
      ,[GuidsText]
  FROM [ErrorLogs] WHERE ParallelId = @parallelId ORDER BY Time

END

