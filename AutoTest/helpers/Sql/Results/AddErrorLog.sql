 INSERT INTO [BO_M_LOGS].[dbo].[ErrorLogs] VALUES 
  (NEWID(), @TestId, @ParallelId, @Text, @Type, @Line, @Time, @Bug, @ScreenPath, @Checked, @GuidText)