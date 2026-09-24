/*This column is depended on column IsAcknowledged.
Logic :- If IsAcknowledged is 0 then notification sound will be played.
		 After playing the sound the PlayNotificationSound will be set to false.
*/

ALTER TABLE [RadioCheckPushMessages]
ADD [PlayNotificationSound] bit default 1
