c:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\InstallUtil.exe PlanServerWinService.exe
net start planserver

sc failure PlanServer reset= 86400 actions= restart/60000/restart/60000/restart/60000
pause