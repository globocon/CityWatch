USE [CityWatchDb]
GO

CREATE TABLE [dbo].[Users] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [UserName] VARCHAR (25)  NOT NULL,
    [Password] VARCHAR (MAX) NOT NULL,
    [IsAdmin]  BIT           NOT NULL
);
GO

