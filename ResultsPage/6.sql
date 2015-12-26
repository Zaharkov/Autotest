
GO
/****** Object:  StoredProcedure [dbo].[DeleteTestInfo]    Script Date: 12/26/2015 19:07:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[DeleteTestInfo](@parallelId uniqueidentifier, @testId uniqueidentifier) as 

BEGIN
	DELETE FROM [ErrorLogs] WHERE ParallelId = @parallelId AND TestId = @testId
	DELETE FROM [TestLogs] WHERE ParallelId = @parallelId AND TestId = @testId
END
