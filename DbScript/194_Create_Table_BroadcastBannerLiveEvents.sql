Create Table BroadcastBannerLiveEvents(id int primary key identity(1,1),TextMessage nvarchar(max), ExpiryDate datetime)
insert into BroadcastBannerLiveEvents(TextMessage,ExpiryDate)values('new day message','01-01-2023')

Create Table BroadcastBannerCalendarEvents(id int primary key identity(1,1),ReferenceNo nvarchar(max),TextMessage nvarchar(max), StartDate datetime,ExpiryDate datetime)
