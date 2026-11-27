alter table guards add IsReactive [bit] default 0
update Guards set IsReactive=0