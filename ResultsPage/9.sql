
GO
/****** Object:  StoredProcedure [dbo].[GetParallelInfo]    Script Date: 12/26/2015 19:10:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[GetParallelInfo](@parallelId uniqueidentifier) as 

BEGIN
SELECT [Id]
      ,[Address]
      ,[TimeStart]
      ,[TimeEnd]
      ,[ScreenPath]
      ,[TestsCount]
  FROM [[ParallelLogs] WHERE Id = @parallelId AND IsDeleted != 1
END
