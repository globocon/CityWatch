/*This column is depended on column IsEnabled.
Logic :- If IsEnabled is 1 then notification sound will be played.
		 After playing the sound the PlayDuressAlarm will be set to false.
*/

ALTER TABLE [ClientSiteDuress]
  ADD PlayDuressAlarm bit Default 1
