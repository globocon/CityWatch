alter table RCActionList add IsRCBypass bit default 0
update RCActionList set IsRCBypass=0


select * from RCActionList