-- =====================================================
-- FitPlay Database Cleanup Script
-- WARNING: This will DELETE ALL booking-related data!
-- =====================================================
-- This script removes all:
--   - RoomCheckIns
--   - ClassEnrollments  
--   - ClassQueueEntries
--   - ClassSchedules
--   - ClassSessions
--   - RoomBookings
-- =====================================================

USE FitPlay; -- Change this to your actual database name if different
GO

PRINT 'Starting database cleanup...';
GO

-- Step 1: Delete RoomCheckIns (references ClassEnrollments)
DECLARE @checkInsCount INT;
SELECT @checkInsCount = COUNT(*) FROM RoomCheckIns;
DELETE FROM RoomCheckIns;
PRINT 'Deleted ' + CAST(@checkInsCount AS VARCHAR(10)) + ' RoomCheckIns';
GO

-- Step 2: Delete ClassEnrollments (references ClassSessions)
DECLARE @enrollmentsCount INT;
SELECT @enrollmentsCount = COUNT(*) FROM ClassEnrollments;
DELETE FROM ClassEnrollments;
PRINT 'Deleted ' + CAST(@enrollmentsCount AS VARCHAR(10)) + ' ClassEnrollments';
GO

-- Step 3: Delete ClassQueueEntries (references ClassSchedules)
DECLARE @queuesCount INT;
SELECT @queuesCount = COUNT(*) FROM ClassQueueEntries;
DELETE FROM ClassQueueEntries;
PRINT 'Deleted ' + CAST(@queuesCount AS VARCHAR(10)) + ' ClassQueueEntries';
GO

-- Step 4: Delete ClassSchedules (references RoomBookings)
DECLARE @schedulesCount INT;
SELECT @schedulesCount = COUNT(*) FROM ClassSchedules;
DELETE FROM ClassSchedules;
PRINT 'Deleted ' + CAST(@schedulesCount AS VARCHAR(10)) + ' ClassSchedules';
GO

-- Step 5: Delete ClassSessions (references RoomBookings)
DECLARE @sessionsCount INT;
SELECT @sessionsCount = COUNT(*) FROM ClassSessions;
DELETE FROM ClassSessions;
PRINT 'Deleted ' + CAST(@sessionsCount AS VARCHAR(10)) + ' ClassSessions';
GO

-- Step 6: Delete RoomBookings (root table)
DECLARE @bookingsCount INT;
SELECT @bookingsCount = COUNT(*) FROM RoomBookings;
DELETE FROM RoomBookings;
PRINT 'Deleted ' + CAST(@bookingsCount AS VARCHAR(10)) + ' RoomBookings';
GO

PRINT 'Database cleanup completed successfully!';
GO

-- Optional: Show remaining counts to verify
SELECT 'RoomCheckIns' AS TableName, COUNT(*) AS RemainingRows FROM RoomCheckIns
UNION ALL
SELECT 'ClassEnrollments', COUNT(*) FROM ClassEnrollments
UNION ALL
SELECT 'ClassQueueEntries', COUNT(*) FROM ClassQueueEntries
UNION ALL
SELECT 'ClassSchedules', COUNT(*) FROM ClassSchedules
UNION ALL
SELECT 'ClassSessions', COUNT(*) FROM ClassSessions
UNION ALL
SELECT 'RoomBookings', COUNT(*) FROM RoomBookings;
GO
