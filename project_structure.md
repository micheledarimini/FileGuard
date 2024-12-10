# Struttura del Progetto FileGuard

## Stato Attuale
Il progetto ha una struttura MVVM con:
- Gestione filesystem (FileSystemManager)
- Interfaccia WPF
- Sistema di persistenza stato (StateManager)
- Factory per creazione nodi (FileSystemNodeFactory)
- Sistema di versionamento Git configurato
- Gestione icone base implementata

### Componenti Principali
- StateManager: gestione persistenza stato
- DirectoryNode: gestione gerarchia e propagazione
- TreeViewModel: coordinamento UI e logica
- FileSystemNodeFactory: creazione nodi con stato
- IconConverter: gestione icone file e cartelle

### Problemi Risolti
1. Implementata gestione stato intermedio (quadratini grigi)
2. Migliorata serializzazione JSON
3. Implementato caricamento ricorsivo nodi
4. Implementata struttura gerarchica per gli stati
5. Migliorata gestione concorrenza
6. Centralizzata logica di gestione stato
7. Implementata regola generale per gestione stati:
   - Modalità Sincronizzazione: durante caricamento/ripristino
   - Modalità Interattiva: durante interazione utente
8. Configurato sistema di versionamento:
   - Repository Git inizializzato e configurato
   - Script di commit (1-commit.bat) funzionante
   - Backup automatico durante i commit
9. Implementata gestione base delle icone:
   - Icone vettoriali per file e cartelle
   - Sistema di conversione in IconConverter
   - Struttura pronta per miglioramenti futuri

### Problemi Pendenti
1. Tree View:
   - Gestione espansione/collasso nodi da migliorare
   - Aggiornamento automatico quando cambiano i file
   - Mantenimento stato espansione tra le sessioni
   - Miglioramento icone per rispecchiare Windows Explorer

2. Parte destra (dettagli):
   - Da implementare visualizzazione dettagli
   - Da gestire aggiornamenti in tempo reale

### Regole Generali
1. Gestione Stati
   - Usare NodeOperationMode per distinguere tra sincronizzazione e interazione
   - Mantenere la coerenza tra IsChecked e MonitoringStatus
   - Propagare gli stati solo in modalità interattiva

2. Struttura Codice
   - Mantenere separazione MVVM
   - Evitare logica duplicata
   - Usare interfacce per disaccoppiamento
   - Centralizzare la gestione dello stato in StateManager

3. Gestione Eventi
   - Propagare eventi solo quando necessario
   - Evitare cicli di aggiornamento
   - Mantenere la gerarchia degli stati

4. Persistenza
   - Salvare lo stato completo dei nodi
   - Validare la gerarchia durante il caricamento
   - Pulire i percorsi non validi

5. Versionamento
   - Usare 1-commit.bat per tutti i commit
   - Verificare che il backup venga creato
   - Mantenere messaggi commit descrittivi

### Note Implementazione
- Focus su regole generali, non casi specifici
- Mantenere codice pulito ed efficiente
- Evitare patch temporanee
- Gestire correttamente le dipendenze
- Documentare le scelte implementative
- Preferire soluzioni semplici e non invasive

## Struttura Directory
```
Core/
├── Cache/
│   ├── StateManager.cs        # Gestione persistenza
│   └── StateManagerConfig.cs  # Configurazione
├── FileSystem/
│   ├── FileSystemEventHandler.cs
│   ├── FileSystemManager.cs   # Gestione filesystem
│   ├── FileSystemMonitor.cs
│   └── FileSystemNodeFactory.cs # Factory nodi
├── Interfaces/
│   ├── IFileSystemEventHandler.cs
│   ├── IFileSystemManager.cs
│   ├── IFileSystemMonitor.cs
│   ├── IFileSystemNodeFactory.cs
│   ├── IStateManager.cs
│   └── ITreeViewModel.cs
├── Models/
│   ├── DirectoryNode.cs       # Nodo directory
│   ├── DummyNode.cs
│   ├── FileNode.cs           # Nodo file
│   ├── FileSystemNode.cs     # Classe base
│   ├── MonitoredPaths.cs     # Modello percorsi
│   └── NodeState.cs          # Stato nodo
├── UI/
│   ├── Components/
│   │   ├── NewFilePopup.xaml
│   │   └── NewFilePopup.xaml.cs
│   ├── Converters/
│   │   ├── IconConverter.cs      # Gestione icone
│   │   └── MonitoringStatusConverter.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
└── ViewModels/
    ├── TreeViewModel.cs       # ViewModel principale
    └── TreeViewModelConfig.cs # Configurazione
