
GO
/****** Object:  StoredProcedure [dbo].[GetTestsInfo]    Script Date: 12/26/2015 19:11:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[GetTestsInfo](@parallelId uniqueidentifier) as 

BEGIN
SELECT A.[TestId]
      ,A.[ParallelId]
      ,A.[TimeStart]
      ,A.[TimeEnd]
      ,B.Name
      ,B.Owner
      ,A.LoginName
  FROM [TestLogs] as A JOIN [AutoTestLog] as B ON A.TestId = B.Id
  WHERE ParallelId = @parallelId ORDER BY A.TimeStart
END

