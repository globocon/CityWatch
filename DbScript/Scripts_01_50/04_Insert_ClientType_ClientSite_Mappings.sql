DELETE FROM ClientSites;
GO

DELETE FROM ClientTypes;
GO

INSERT INTO ClientTypes VALUES ('Automotive');
INSERT INTO ClientTypes VALUES ('Health & Hospitals');
INSERT INTO ClientTypes VALUES ('Hotels & Accomodation')
INSERT INTO ClientTypes VALUES ('Industrial Sites')
INSERT INTO ClientTypes VALUES ('Local Council & Leisure Centre')
INSERT INTO ClientTypes VALUES ('Major Events & Concerts')
INSERT INTO ClientTypes VALUES ('Mobile Patrol Car (Adhoc)')
INSERT INTO ClientTypes VALUES ('Pubs & Nightclubs')
INSERT INTO ClientTypes VALUES ('Retail, Jewellery & Fashion Store')
INSERT INTO ClientTypes VALUES ('Shopping Centres & Markets')
INSERT INTO ClientTypes VALUES ('Schools, Library, & Educational')
INSERT INTO ClientTypes VALUES ('Transport Company')
INSERT INTO ClientTypes VALUES ('VISY-QLD')
INSERT INTO ClientTypes VALUES ('VISY-NSW')
INSERT INTO ClientTypes VALUES ('VISY-VIC')
INSERT INTO ClientTypes VALUES ('Zoo & Animals')
INSERT INTO ClientTypes VALUES ('Other, ADHOC, & Private Clients')
INSERT INTO ClientTypes VALUES ('N/A')
GO

INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Automotive'), 'Nunawading Hyundai');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Automotive'), 'Pickles Car Auctions');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Austin-Heidelberg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Camms Road Maternal & Child Health');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Royal Children''s Hospital');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Mercy-Heidelberg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Health & Hospitals'), 'Mercy-Werribee');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Hotels & Accomodation'), 'CitiPlan');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Consolidated Metals');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'CSR Gyprock');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Hoffman Brickworks');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Norstar Steel Recyclers');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Industrial Sites'), 'Victorian Chemical');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'City of Casey');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'Cranbourne Leisure Centre');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Local Council & Leisure Centre'), 'Endeavour Hills Leisure Centre');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'Avalon AirShow');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'F1 GrandPrix');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'Melbourne Cup');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Major Events & Concerts'), 'New Years Eve');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Bulgari');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Channel');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Crown Casino');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Leonard Joel');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'L''Oreal');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Retail, Jewellery & Fashion Store'), 'Revlon');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Caribbean Gardens');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Chadstone Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Churinga Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Dalton Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Ferntree Plaza Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Highpoint Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Hive Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Milleara Mall');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Shopping Centres & Markets'), 'Niddrie Central Shopping');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Doveton Library');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Melbourne University');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Notre Dame Uni');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Polytechnic');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Schools, Library, & Educational'), 'Vermont South Special School');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Arrow');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Chemtrans');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Jayco Caravans');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Transport Company'), 'Mainfreight');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Carrara');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Gibson Island');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Maroochydore');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-QLD'), 'VISY-Rocklea');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Albury');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Smithfield');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Warrick Farm');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-NSW'), 'VISY-Wollongong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Banyule');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Clayton');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Coolaroo');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Coburg');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Dandenong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Geelong');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Kilsyth');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Laverton');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Reservoir');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Springvale');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'VISY-VIC'), 'VISY-Truganina');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Lort-Smith Animal Hospital');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Melbourne Zoo');
INSERT INTO ClientSites VALUES ((SELECT Id FROM ClientTypes WHERE [Name] = 'Zoo & Animals'), 'Werribee Zoo');
GO