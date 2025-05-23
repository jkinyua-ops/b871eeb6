-- Create Nostra_Dataload database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Nostra_Dataload')
BEGIN
    CREATE DATABASE Nostra_Dataload;
END
GO

USE Nostra_Dataload;
GO

-- Create Nostra_Dataload_Tasks database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Nostra_Dataload_Tasks')
BEGIN
    CREATE DATABASE Nostra_Dataload_Tasks;
END
GO

USE Nostra_Dataload_Tasks;
GO