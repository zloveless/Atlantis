@echo off
cd %~dp0
SET CONFIG=""

IF NOT EXIST "%~dp0\nuget" (
    echo [*] Creating nuget\ directory...
    mkdir "%~dp0\nuget"
)

IF "%1"=="" (
    SET CONFIG=Debug
) ELSE (
    SET CONFIG=%1
)

for /f "tokens=*" %%G in ('dir /b /a:d-h Code ^| findstr /v /i "packages"') do (
    echo [*] Packing %%G for %CONFIG% build...
    nuget pack Code\%%G\%%G.csproj -Properties Configuration=%CONFIG% -IncludeReferencedProjects -OutputDirectory %~dp0nuget\
)
