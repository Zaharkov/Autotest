
GO
/****** Object:  StoredProcedure [dbo].[ChangeParallelScreenPath]    Script Date: 12/26/2015 19:05:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[ChangeParallelScreenPath](@parallelId uniqueidentifier, @screenPath nvarchar(256)) as 

BEGIN
	UPDATE [ParallelLogs] SET screenPath = @screenPath WHERE Id = @parallelId
END
