DELETE FROM [BO_M_LOGS].[dbo].[ErrorLogs] WHERE TestId = @TestId AND ParallelId = @ParallelId
DELETE FROM [BO_M_LOGS].[dbo].[TestLogs] WHERE TestId = @TestId AND ParallelId = @ParallelId