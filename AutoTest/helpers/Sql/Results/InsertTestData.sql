IF (SELECT COUNT(*) FROM [BO_M_LOGS].[dbo].[AutoTestLog] WITH (NOLOCK) WHERE Id = @Id) = 0
	  INSERT INTO [BO_M_LOGS].[dbo].[AutoTestLog]
	  (Id, Name, Time, CriticalError, SimpleError, isTestValid, Description, Bugs, EndTime, Owner)
	  VALUES  (@Id, @Name, @Time, @CriticalError, @SimpleError, @IsTestValid, '', '', @EndTime, @Owner)
ELSE
	UPDATE [BO_M_LOGS].[dbo].[AutoTestLog]
	SET Name = @Name,
		Time = (Time * 4 + @Time) / 5,
		CriticalError = @CriticalError,
		SimpleError = @SimpleError,
		Description = '',
		Bugs = '',
		EndTime = @EndTime,
		Owner = @Owner
	WHERE Id = @Id