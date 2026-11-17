Create table RadioCheckStatusColor( Id int primary key identity(1,1) ,Name nvarchar(max))
insert into RadioCheckStatusColor(name) values('Red 1')
insert into RadioCheckStatusColor(name) values('Red 2')
insert into RadioCheckStatusColor(name) values('Red 3')
insert into RadioCheckStatusColor(name) values('Green 1')


Create table RadioCheckStatus( Id int primary key identity(1,1) ,ReferenceNo nvarchar(max),Name nvarchar(max),RadioCheckStatusColorId int)
ALTER TABLE [dbo].[RadioCheckStatus]  WITH CHECK ADD FOREIGN KEY([RadioCheckStatusColorId])
REFERENCES [dbo].[RadioCheckStatusColor] ([Id])
ALTER TABLE [dbo].[RadioCheckStatus] add RadioCheckStatusColorName nvarchar(max)
SET IDENTITY_INSERT [dbo].[RadioCheckStatus] ON 
GO
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (1, N'01', N'N/A', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (2, N'02', N'Off Duty', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (3, N'03', N'N/A Only W/E', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (4, N'04', N'N/A Only Summer', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (5, N'05', N'On Standby', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (6, N'06', N'Incoming Call', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (7, N'07', N'Outgoing Call Radio', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (8, N'08', N'Outgoing Call Mobile', NULL, NULL)
INSERT [dbo].[RadioCheckStatus] ([Id], [ReferenceNo], [Name], [RadioCheckStatusColorId], [RadioCheckStatusColorName]) VALUES (10, N'10', N'No Answer', NULL, NULL)
GO
SET IDENTITY_INSERT [dbo].[RadioCheckStatus] OFF


alter table ClientSiteRadioChecks add RadioCheckStatusId int
ALTER TABLE [dbo].[ClientSiteRadioChecks]  WITH CHECK ADD FOREIGN KEY([RadioCheckStatusId])
REFERENCES [dbo].[RadioCheckStatus] ([Id])




