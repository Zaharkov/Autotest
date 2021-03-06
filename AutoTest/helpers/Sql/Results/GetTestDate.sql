SELECT TestId as Id, AVG(time.Time) as Time
 FROM [BO_M_LOGS].[dbo].[TestLogs] a
 cross apply (SELECT TOP 5 DATEDIFF(second, TimeStart, TimeEnd) as Time FROM [BO_M_LOGS].[dbo].[TestLogs] b
 WHERE a.TestId = b.TestId and b.TimeEnd is not NULL ORDER BY b.TimeStart DESC) time
 GROUP BY TestId