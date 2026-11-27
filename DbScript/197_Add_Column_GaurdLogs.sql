/*This column is depended on column RcPushMessageId.
Logic :- If RcPushMessageId is not null then notification sound will be played.
		 After playing the sound the PlayNotificationSound will be set to false.
*/

ALTER TABLE [GuardLogs]
ADD [PlayNotificationSound] bit default 1
