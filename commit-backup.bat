@echo off
:: Prende la data in formato YYYYMMDD
set datestamp=%date:~6,4%%date:~3,2%%date:~0,2%

:: Crea cartella backup se non esiste
if not exist "D:\FileGuard_Backup" mkdir "D:\FileGuard_Backup"

:: Chiede il messaggio del commit
set /p commit_msg="Messaggio del commit: "

:: Add e commit su Git
git add .
git commit -m "%commit_msg%"

:: Crea lo zip con data
powershell Compress-Archive -Path D:\FileGuard\* -DestinationPath "D:\FileGuard_Backup\FileGuard_%datestamp%.zip" -Force

echo Commit e backup completati!
pause