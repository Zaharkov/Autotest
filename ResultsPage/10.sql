
GO
/****** Object:  StoredProcedure [dbo].[GetParallelsInfo]    Script Date: 12/26/2015 19:10:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[GetParallelsInfo] as 

BEGIN
SELECT [Id]
      ,[Address]
      ,[TimeStart]
      ,[TimeEnd]
      ,[ScreenPath]
      ,[TestsCount]
  FROM [ParallelLogs] WHERE IsDeleted != 1 ORDER BY TimeStart DESC
END
