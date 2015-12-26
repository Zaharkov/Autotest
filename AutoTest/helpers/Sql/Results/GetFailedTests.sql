SELECT DISTINCT A.Name FROM 
[BO_M_LOGS].[dbo].[AutoTestLog] A
INNER JOIN [BO_M_LOGS].[dbo].[ErrorLogs] B
ON A.Id = B.TestId
WHERE B.Checked = '0' AND ParallelId = @ParallelId