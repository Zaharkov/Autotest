
GO
/****** Object:  StoredProcedure [dbo].[DeleteParallelInfo]    Script Date: 12/26/2015 19:07:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[DeleteParallelInfo](@parallelId uniqueidentifier) as 

BEGIN
	UPDATE [ParallelLogs] SET IsDeleted = 1 WHERE Id = @parallelId
END
