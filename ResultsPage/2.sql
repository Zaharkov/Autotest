
GO
/****** Object:  StoredProcedure [dbo].[CheckErrorInfo]    Script Date: 12/26/2015 19:06:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[CheckErrorInfo](@errorId uniqueidentifier) as 

BEGIN
	UPDATE [ErrorLogs] SET Checked = 1 WHERE Id = @errorId
END
