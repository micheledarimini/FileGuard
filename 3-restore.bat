@echo off
setlocal EnableDelayedExpansion

cls
echo ===============================================
echo     3. RIPRISTINA VERSIONE PRECEDENTE
echo ===============================================
echo.
echo ATTENZIONE: Prima di ripristinare una versione,
echo assicurati di aver salvato le modifiche correnti
echo usando .\1-commit.bat
echo.
echo Premi un tasto per vedere le versioni disponibili...
pause > nul

cls
echo ===============================================
echo            VERSIONI DISPONIBILI
echo ===============================================
echo.
git log --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s%%n-----------------------------------------------" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S"
echo.
echo.
echo ===============================================
echo               RIPRISTINO
echo ===============================================
echo.
echo Per ripristinare una versione:
echo - Copia l'ID del commit desiderato (primi 7 caratteri)
echo - Incollalo quando richiesto
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
    echo Usa .\2-show.bat per vedere gli ID disponibili.
    pause
    exit /b
)

:: Mostra dettagli del commit selezionato
echo.
echo DETTAGLI VERSIONE SELEZIONATA:
git log -1 --pretty=format:"ID:    %%h%%nData:  %%ad%%nMsg:   %%s" --date=format:"%%d/%%m/%%Y %%H:%%M:%%S" !commit_id!
echo.
echo.
echo ATTENZIONE: 
echo - Questa operazione riporterà TUTTI i file allo
echo   stato della versione selezionata
echo - Le modifiche non salvate andranno PERSE
echo - Non sarà possibile annullare l'operazione
echo.
set /p confirm="Sei SICURO di voler ripristinare questa versione? (S/N): "
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
echo La versione !commit_id! è stata ripristinata.
echo.
echo Per verificare lo stato attuale:
echo   .\2-show.bat
echo.
echo Per salvare nuove modifiche:
echo   .\1-commit.bat
echo.
pause
