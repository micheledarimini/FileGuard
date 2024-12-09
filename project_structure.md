# Struttura del Progetto FileGuard

## Stato Attuale
Il progetto ha una struttura MVVM con:
- Gestione filesystem (FileSystemManager)
- Interfaccia WPF
- Sistema di persistenza stato (StateManager)
- Factory per creazione nodi (FileSystemNodeFactory)

### Componenti Principali
- StateManager: gestione persistenza stato
- DirectoryNode: gestione gerarchia e propagazione
- TreeViewModel: coordinamento UI e logica
- FileSystemNodeFactory: creazione nodi con stato

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

### Problemi Pendenti
1. Incoerenza nella gestione degli stati:
   - Gli stati vengono ripristinati ma non propagati correttamente
   - I quadratini non si aggiornano in modo coerente
   - Le selezioni non vengono mantenute tra le sessioni

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

### Note Implementazione
- Focus su regole generali, non casi specifici
- Mantenere codice pulito ed efficiente
- Evitare patch temporanee
- Gestire correttamente le dipendenze
- Documentare le scelte implementative

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
│   │   └── MonitoringStatusConverter.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
└── ViewModels/
    ├── TreeViewModel.cs       # ViewModel principale
    └── TreeViewModelConfig.cs # Configurazione
