set objId=48907
set g_user_id=7172
set pow_batch=100
set oscid=EupA0S%%2BLlRps2qK%%2BvOCFLum1%%2BCv88Wm0L6LSTjShgc206yB6BHMqwvaw9Vj6DPdFreTNHljBVX%%2F%%2BbOC5hyTCAo4obVsdTgn91vvWi4sSZRbeTv6pN9afrAUBJScwv9pgQNQLFrKKiy3HyxoeAU9e6NoXVvaWBh95FgxptmOmVZA%%3D



@echo off
SET /A "index=1"
SET /A "count=10000"
:while
if %index% leq %count% (
 oschina2022.exe
   SET /A "index=index + 1"
   goto :while
)
 