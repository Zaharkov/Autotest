
GO
/****** Object:  StoredProcedure [dbo].[DeleteErrorInfo]    Script Date: 12/26/2015 19:07:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[DeleteErrorInfo](@errorId uniqueidentifier) as 

BEGIN
	DELETE FROM [ErrorLogs] WHERE Id = @errorId
END
