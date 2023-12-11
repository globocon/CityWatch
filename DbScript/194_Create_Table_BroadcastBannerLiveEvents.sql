Create Table BroadcastBannerLiveEvents(id int primary key identity(1,1),ReferenceNo nvarchar(max),TextMessage nvarchar(max), ExpiryDate datetime)
insert into BroadcastBannerLiveEvents(ReferenceNo,TextMessage,ExpiryDate)values('01','new day message','01-01-2023')


