# Stato Build FileGuard - 28/11/2024

## 1. STATO ATTUALE

### File e Percorsi Coinvolti
- D:\FileGuard\Core\FileGuard.csproj (progetto principale)
- D:\FileGuard\Build\Release (directory output build)
- D:\FileGuard\Core\bin e obj (cache build)
- D:\FileGuard\.gitignore (configurazione git)
- D:\FileGuard\status.md (documentazione stato)
- D:\FileGuard\design.md (documentazione design)

### Punto Esatto del Progresso
- Build non funzionante dopo tentativo di ripristino git
- Ultimo comando eseguito:
```bash
dotnet publish "Core\FileGuard.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "Build\Release"
```

### Stato Componenti
- Funzionante:
  * Repository git (struttura base)
  * File sorgente ripristinati
  * .gitignore configurato correttamente

- Non Funzionante:
  * Build singolo file (genera DLL multiple)
  * Output directory non contiene file attesi

- Errori Specifici:
  * Build genera file esterni alla cartella target
  * Possibile corruzione delle cache di build

### Decisioni Prese
1. Tentato ripristino git per tornare a stato pulito
2. Mantenuto .gitignore originale
3. Provate diverse opzioni di build senza successo

### Dipendenze
- WPF (richiede DLL specifiche)
- .NET 8.0 SDK
- Dipendenze native Windows

## 2. TENTATIVI PRECEDENTI

### Approcci Falliti
1. Build Standard:
```bash
dotnet publish -c Release
```
- Risultato: Multiple DLL generate

2. Single File con compressione:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```
- Risultato: Ancora DLL separate

3. Con trimming:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```
- Risultato: Errore NETSDK1175 (WPF non supporta trimming)

4. Con IncludeAllContentForSelfExtract:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
```
- Risultato: File generati in posizioni errate

### Lezioni Apprese
1. WPF limita le opzioni di ottimizzazione
2. Trimming non utilizzabile
3. Necessario gestire DLL native
4. Importante pulire cache prima del build

### Test Effettuati
1. Build con diverse configurazioni
2. Verifica file generati
3. Tentativi di pulizia cache
4. Ripristino git

## 3. ANALISI TECNICA

### Pattern Problematici
1. Cache build persistente
2. DLL WPF non incluse nel single file
3. Percorsi output non rispettati

### Conflitti Dipendenze
1. WPF vs Single File Publishing
2. Native DLL vs Compression

### Problemi Performance
1. Build genera file eccessivi
2. Cache potenzialmente corrotta

### Punti Critici
1. Gestione DLL native
2. Output path management
3. Cache cleaning

## 4. SUGGERIMENTI PER PROSSIMO TASK

1. Verificare configurazione SDK .NET 8
2. Pulire COMPLETAMENTE ambiente prima del build:
   - Rimuovere cartelle bin/obj
   - Pulire cache NuGet
   - Rimuovere output precedenti
3. Usare percorsi assoluti nel comando build
4. Verificare output dopo ogni comando
5. NON usare trimming (incompatibile con WPF)
6. Considerare approccio alternativo per single file

## 5. DOCUMENTAZIONE

### Link Rilevanti
- [Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file)
- [WPF limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)

### Configurazioni Importanti
```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

### Variabili Ambiente
- DOTNET_ROOT
- PATH (include .NET SDK)
- NUGET_PACKAGES

### Note Finali
Il problema principale sembra essere la gestione delle DLL native di WPF nel contesto di single-file publishing. Il prossimo task dovrebbe concentrarsi su questo aspetto specifico.
