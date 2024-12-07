@echo off
setlocal EnableDelayedExpansion

cls
echo ===============================================
echo            RIPRISTINO VERSIONE
echo ===============================================
echo.
echo VERSIONI DISPONIBILI:
echo.
git log --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s%%n-----------------------------------------------" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S"
echo.
echo.
echo BACKUP ZIP DISPONIBILI:
echo.
for %%F in ("D:\FileGuard_Backup\*.zip") do (
    echo File:    %%~nxF
    echo Data:    %%~tF
    echo -----------------------------------------------
)
echo.
echo ===============================================
echo               RIPRISTINO
echo ===============================================
echo.
echo Per ripristinare una versione Git:
echo - Inserisci l'ID del commit (i primi 7 caratteri)
echo - Esempio: per ripristinare ID: ae91e35, scrivi: ae91e35
echo.
echo Per annullare: premi INVIO senza inserire nulla
echo.
set /p commit_id="ID commit da ripristinare: "
if "!commit_id!"=="" (
    echo.
    echo Operazione annullata.
    pause
    exit /b
)

:: Verifica che il commit esista
git rev-parse --verify !commit_id! >nul 2>&1
if errorlevel 1 (
    echo.
    echo ERRORE: Il commit ID !commit_id! non esiste.
    echo Usa .\show-versions.bat per vedere gli ID disponibili.
    pause
    exit /b
)

:: Mostra dettagli del commit selezionato
echo.
echo DETTAGLI COMMIT SELEZIONATO:
git log -1 --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S" !commit_id!
echo.
echo.
echo ATTENZIONE: Il ripristino riporterà tutti i file allo stato
echo            di questa versione. Le modifiche non salvate
echo            andranno perse.
echo.
set /p confirm="Confermi il ripristino? (S/N): "
if /i not "!confirm!"=="S" (
    echo.
    echo Operazione annullata.
    pause
    exit /b
)

:: Esegue il ripristino
echo.
echo Ripristino in corso...
git reset --hard !commit_id!

echo.
echo ===============================================
echo            RIPRISTINO COMPLETATO
echo ===============================================
echo.
echo La versione !commit_id! è stata ripristinata con successo.
echo.
echo Per verificare lo stato attuale usa: .\show-versions.bat
echo.
pause
