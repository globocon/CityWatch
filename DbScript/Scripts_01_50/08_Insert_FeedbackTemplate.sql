USE CityWatchDb
GO

INSERT INTO [FeedbackTemplates] VALUES ('DATA Error - No Internet', 'I started my shift @ 00:00 hrs on YYYYMMDD.
I noticed @ 00:00 hrs we have experienced and internet connection issue.
The FLIR images are on the Laptop Dropbox; but we have no internet because the Smart WAND wont connect to cloud.');

INSERT INTO [FeedbackTemplates] VALUES ('Emergency Services on Site', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed an Alarm sounding from 
I called 
I then
At approx. 00:00 hrs a fire truck arrived (number plate XXX-XXX) 
I then
At approx. 00:00 hrs a fire truck departed (number plate XXX-XXX) 
I then
I also took photos of the panel / fire trucks / event');

INSERT INTO [FeedbackTemplates] VALUES ('Fire Equipment - Alarm', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed an Alarm sounding from the ASE / FIP / VESDA which is located in
I then
I also took a photo of the');

INSERT INTO [FeedbackTemplates] VALUES ('Fire Equipment - Fault', 'I started my shift @ 00:00 hrs on YYYYMMDD.
At approx 00:00 hrs I noticed a Fault or Error on the ASE / FIP / VESDA which is located in
I then
I also took a photo of the');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – EXT. Only - All ok', 'An External patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – INT. Only - All ok', 'An internal patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Patrol – EXT + INT Only - All ok', 'Both an external and an internal patrol was undertaken on the clients site; 
No offence or damage was detected;
No one was found on site;
All is ok; Site was secured on departure.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol - CLIENT on site', 'I started my shift @ 00:00 hrs on YYYYMMDD.
The client is working on site and hence we can not conduct a full patrol.
The client left @ 00:00 and we then continued our shift like normal.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol – Inclement Weather - Hot', 'I started my shift @ 00:00 hrs on YYYYMMDD.
It was an extremely hot day, the temperature was XX oC
We reduced our patrols due to OH&S reasons however remained vigilant in case of any fire risks.');

INSERT INTO [FeedbackTemplates] VALUES ('Unable to Patrol – Inclement Weather - Raining', 'I started my shift @ 00:00 hrs on YYYYMMDD.
It was a very wet day, with heavy rain. 
We reduced our patrols due to OH&S reasons.');

GO