@echo off
echo ===============================================
echo            VERSIONI GIT DISPONIBILI
echo ===============================================
echo.
git log --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s%%n-----------------------------------------------" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S"
echo.
echo.
echo ===============================================
echo            BACKUP ZIP DISPONIBILI
echo ===============================================
echo.
echo Directory: D:\FileGuard_Backup
echo.
for %%F in ("D:\FileGuard_Backup\*.zip") do (
    echo File:    %%~nxF
    echo Data:    %%~tF
    echo Size:    %%~zF bytes
    echo -----------------------------------------------
)
echo.
echo Per ripristinare una versione:
echo 1. Usa il comando .\restore-version.bat
echo 2. Inserisci l'ID del commit (i primi 7 caratteri, es: ae91e35)
echo.
pause
