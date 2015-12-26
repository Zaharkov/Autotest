IF (SELECT COUNT(*) FROM [BO_M_LOGS].[dbo].[AutoTestLog] WITH (NOLOCK) WHERE Id = @Id) = 0
	  INSERT INTO [BO_M_LOGS].[dbo].[AutoTestLog]
	  (Id, Name, Time, CriticalError, SimpleError, isTestValid, Description, Bugs, EndTime, Owner)
	  VALUES  (@Id, @Name, '30', '0', '0', '1', '', '', GETDATE(), @Owner)