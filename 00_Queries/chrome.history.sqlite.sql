




--paths
--C:\Users\Antoni Wota\AppData\Local\Google\Chrome\User Data\Profile 3\History   --librus
--C:\Users\Antoni Wota\AppData\Local\Google\Chrome\User Data\Profile 1\History   --normalne   

--SELECT *
select
datetime(v.visit_time/1000000-11644473600,'unixepoch') as data,
u.url, 
u.title
from 
visits v,
urls u
where v.url = u.id 
and u.title not like '%youtube%'
and u.title not like '%allegro%'
and u.title not like '%pokemon%'  and u.title not like '%Pokémon%'
and u.title not like '%bankowoœæ%'
and u.url not like '%mail.google.com%'
order by visit_time desc;


select 
datetime(v.visit_time/1000000-11644473600,'unixepoch') as data,
u.url as adres,
u.id as url_id
from visits as v, urls as u
where v.url = u.id
 order by v.visit_time desc;

 
--select visit_time from visits order by visit_time desc;



Select * From urls;

SELECT name FROM sqlite_schema WHERE type='table' ORDER BY name
SELECT name FROM sqlite_schema

PRAGMA journal_mode



