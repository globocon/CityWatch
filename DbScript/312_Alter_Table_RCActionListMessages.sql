
alter table RCActionListMessages add Endmessagetime datetime
alter table RCActionListMessages add Radiofrequencystatus nvarchar(max)
update RCActionListMessages set Radiofrequencystatus='OnceOff'
