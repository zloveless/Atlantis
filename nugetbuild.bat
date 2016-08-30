@echo off
cd %~dp0
SET CONFIG=%1

for /f "tokens=*" %%G in ('dir /b /a:d-h Code ^| findstr /v /i "packages"') do (
    echo [*] Packing %%G
    nuget pack Code\%%G\%%G.csproj -Properties Configuration=%CONFIG% -IncludeReferencedProjects -OutputDirectory %~dp0nuget\
)
