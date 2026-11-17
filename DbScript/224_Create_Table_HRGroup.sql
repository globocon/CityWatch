create table HRGroups(Id int primary key identity(1,1),Name nvarchar(max),IsDeleted bit)

insert into HRGroups(Name,IsDeleted) values('HR1',0)
insert into HRGroups(Name,IsDeleted) values('HR2',0)
insert into HRGroups(Name,IsDeleted) values('HR3',0)

create table ReferenceNoNumbers(Id int primary key identity(1,1),Name nvarchar(max),IsDeleted bit)

insert into ReferenceNoNumbers(Name,IsDeleted) values('01',0)
insert into ReferenceNoNumbers(Name,IsDeleted) values('02',0)
insert into ReferenceNoNumbers(Name,IsDeleted) values('03',0)

create table ReferenceNoAlphabets(Id int primary key identity(1,1),Name nvarchar(max),IsDeleted bit)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('a',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('b',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('c',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('d',0)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('e',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('f',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('g',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('h',0)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('i',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('j',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('k',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('l',0)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('m',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('n',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('o',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('p',0)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('q',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('r',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('s',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('t',0)

insert into ReferenceNoAlphabets(Name,IsDeleted) values('u',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('v',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('w',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('x',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('y',0)
insert into ReferenceNoAlphabets(Name,IsDeleted) values('z',0)



CREATE TABLE HrSettings(Id int primary key identity(1,1),Description nvarchar(max),HRGroupId int,ReferenceNoNumberId int,ReferenceNoAlphabetId int)

ALTER TABLE [dbo].[HrSettings]  WITH CHECK ADD FOREIGN KEY([HRGroupId])
REFERENCES [dbo].[HRGroups] ([Id])

ALTER TABLE [dbo].[HrSettings]  WITH CHECK ADD FOREIGN KEY([ReferenceNoNumberId])
REFERENCES [dbo].[ReferenceNoNumbers] ([Id])

ALTER TABLE [dbo].[HrSettings]  WITH CHECK ADD FOREIGN KEY([ReferenceNoAlphabetId])
REFERENCES [dbo].[ReferenceNoAlphabets] ([Id])

create table LicenseTypes(Id int primary key identity(1,1),Name nvarchar(max),IsDeleted bit)

insert into LicenseTypes(Name)values('Bodyguard')
insert into LicenseTypes(Name)values('Crowd Controller')
insert into LicenseTypes(Name)values('Driver (Boat)')
insert into LicenseTypes(Name)values('Driver (Car)')
insert into LicenseTypes(Name)values('Firearm')
insert into LicenseTypes(Name)values('Investigator')
insert into LicenseTypes(Name)values('Other')
insert into LicenseTypes(Name)values('Security Guard')

update LicenseTypes set IsDeleted=0





