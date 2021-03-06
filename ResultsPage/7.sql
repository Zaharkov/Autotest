
GO
/****** Object:  StoredProcedure [dbo].[GetErrorsInfo]    Script Date: 12/26/2015 19:09:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[GetErrorsInfo](@parallelId uniqueidentifier, @testId uniqueidentifier) as 

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
  FROM [ErrorLogs] WHERE ParallelId = @parallelId AND TestId = @testId ORDER BY Time

END

