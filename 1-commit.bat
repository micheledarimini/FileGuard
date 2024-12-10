@echo off
setlocal EnableDelayedExpansion

:: Imposta codifica UTF-8
chcp 65001 > nul

cls
echo ===============================================
echo     1. SALVA MODIFICHE (COMMIT E BACKUP)
echo ===============================================
echo.
echo STATO ATTUALE:
echo.
git status
echo.
echo ===============================================

:: Prende la data e ora completa (YYYYMMDD_HHMMSS)
set year=%date:~6,4%
set month=%date:~3,2%
set day=%date:~0,2%
set hour=%time:~0,2%
set minute=%time:~3,2%
set second=%time:~6,2%
:: Rimuove lo spazio iniziale se l'ora è < 10
if "%hour:~0,1%"==" " set hour=0%hour:~1,1%
set timestamp=%year%%month%%day%_%hour%%minute%%second%

:: Chiede il messaggio del commit
echo Inserisci un messaggio descrittivo per il commit
echo Esempio: "Aggiunta funzionalità di ricerca"
echo.
set /p commit_msg="Messaggio: "
if "!commit_msg!"=="" (
    echo.
    echo ERRORE: Il messaggio del commit non può essere vuoto.
    pause
    exit /b
)

:: Esegue le operazioni git
echo.
echo ESECUZIONE COMMIT E PUSH:
echo.
git add .
git commit -m "!commit_msg!"
<<<<<<< HEAD
git push origin main
=======
git push origin master
>>>>>>> 00288fc (Primo commit)

:: Crea cartella backup se non esiste
if not exist "D:\FileGuard_Backup" mkdir "D:\FileGuard_Backup"

echo.
echo CREAZIONE BACKUP ZIP:
echo.

:: Crea lo zip usando PowerShell con bypass della policy
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path 'D:\FileGuard\*' -DestinationPath 'D:\FileGuard_Backup\FileGuard_!timestamp!.zip' -Force"

echo.
echo ===============================================
echo            OPERAZIONI COMPLETATE
echo ===============================================
echo.
echo 1. Commit locale eseguito
echo 2. Push su GitHub completato
echo 3. Backup creato: FileGuard_!timestamp!.zip
echo.
echo DETTAGLI COMMIT:
git log -1 --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S"
echo.
echo.
echo Per vedere tutte le versioni usa: .\2-show.bat
echo.
pause
