GO
/****** Object:  StoredProcedure [dbo].[UnCheckErrorInfo]    Script Date: 12/26/2015 19:11:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[UnCheckErrorInfo](@errorId uniqueidentifier) as 

BEGIN
	UPDATE [ErrorLogs] SET Checked = 0 WHERE Id = @errorId
END
