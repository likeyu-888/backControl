%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe EliteCloudService.exe
Net Start EliteCloudService
sc config EliteCloudService start= auto
