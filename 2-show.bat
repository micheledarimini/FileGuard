@echo off
cls
echo ===============================================
echo     2. VISUALIZZA VERSIONI DISPONIBILI
echo ===============================================
echo.
echo VERSIONI GIT:
echo (Dal più recente al più vecchio)
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
echo ===============================================
echo               COME PROCEDERE
echo ===============================================
echo.
echo Per salvare una nuova versione:
echo   .\1-commit.bat
echo.
echo Per ripristinare una versione:
echo   .\3-restore.bat
echo   (ti verrà chiesto quale versione ripristinare)
echo.
pause
