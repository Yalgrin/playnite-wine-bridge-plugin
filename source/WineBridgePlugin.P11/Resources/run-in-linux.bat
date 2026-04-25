rem ---
set encodedScript=%1
set encodedScript=%encodedScript:~1,-1%
set linuxScript=%2
set linuxScript=%linuxScript:~1,-1%
set correlationId=%3
set correlationId=%correlationId:~1,-1%
set asyncTracking=%4
set asyncTracking=%asyncTracking:~1,-1%
set trackingExpression=%5
set trackingExpression=%trackingExpression:~1,-1%
set trackingDirectory=%6
set trackingDirectory=%trackingDirectory:~1,-1%

cmd /c start /unix %linuxScript% %encodedScript% %correlationId% %asyncTracking% %trackingExpression% %trackingDirectory%
