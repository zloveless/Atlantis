@echo off
cd %~dp0

echo [*] Pushing *.nupkg's inside nuget\ folder...
nuget push nuget\*.nupkg -Source https://www.nuget.org/api/v2/package -ConfigFile %APPDATA%\NuGet\NuGet.config

echo [*] Cleaning up...

for /f "tokens=*" %%G in ('dir /b /a:-h nuget') do (
    echo [*] Deleting %%G
    del nuget\%%G
)
