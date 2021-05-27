%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe EliteService.exe
Net Start EliteService
sc config EliteService start= auto