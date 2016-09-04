@echo off
cd %~dp0

for /f "tokens=*" %%G in ('dir /b /a:-h nuget') do (
    echo [*] Pushing %%G
    echo "nuget push nuget\%%G"
    
    REM Wait..
    timeout /T 1 >nul
    echo [*] Deleting %%G
    echo "del nuget\%%G"
)
