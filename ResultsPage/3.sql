
GO
/****** Object:  StoredProcedure [dbo].[CheckTestInfo]    Script Date: 12/26/2015 19:07:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[CheckTestInfo](@parallelId uniqueidentifier, @testId uniqueidentifier) as 

BEGIN
	UPDATE [ErrorLogs] SET Checked = 1 WHERE ParallelId = @parallelId AND TestId = @testId
END
