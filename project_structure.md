# Struttura del Progetto FileGuard

## Organizzazione Generale
Il progetto segue l'architettura MVVM con una chiara separazione tra UI, logica di business e modelli dati.

## Struttura Directory e Componenti

### Core/Cache/
- `StateManager.cs`: Gestione persistenza stato dell'applicazione
  * Integra configurazione e gestione stato
  * Salva/carica stati di monitoraggio
  * Dipende da: Models/NodeState, Interfaces/IStateManager

### Core/FileSystem/
- `ChangeTracker.cs`: Tracciamento modifiche filesystem
- `FileSystemEventHandler.cs`: Gestione eventi filesystem
- `FileSystemEventProcessor.cs`: Elaborazione eventi e aggiornamento UI
  * Gestisce la coda eventi (max 1000)
  * Crea FileChangeInfo per ogni evento
  * Aggiorna nodi UI preservando stati
- `FileSystemManager.cs`: Gestione operazioni filesystem
- `FileSystemMonitor.cs`: Monitoraggio cambiamenti filesystem
- `FileSystemNodeFactory.cs`: Factory per creazione nodi

### Core/Interfaces/
- Interfacce per disaccoppiamento componenti:
  * `IChangeTracker.cs`: Tracciamento modifiche
  * `IFileSystemEventHandler.cs`: Eventi filesystem
  * `IFileSystemManager.cs`: Operazioni filesystem
  * `IFileSystemMonitor.cs`: Monitoraggio
  * `IFileSystemNodeFactory.cs`: Creazione nodi
  * `INotificationService.cs`: Notifiche
  * `ISelectionManager.cs`: Gestione selezioni
  * `IStateManager.cs`: Gestione stato
  * `ITreeNode.cs`: Nodi albero
  * `ITreeViewModel.cs`: ViewModel principale

### Core/Logging/
Sistema logging autonomo:
- `FileLogger.cs`: Implementazione logger su file
- `ILogger.cs`: Interfaccia logging
- `Logger.cs`: Classe base logger
- `LoggerConfiguration.cs`: Configurazione logging
- `LoggerFactory.cs`: Factory per logger

### Core/Models/
Modelli dati dell'applicazione:
- `DirectoryNode.cs`: Rappresentazione cartelle
- `DummyNode.cs`: Nodo placeholder
- `FileChangedEventArgs.cs`: Eventi cambio file
- `FileChangeInfo.cs`: Info modifiche filesystem
  * Timestamp, Path, Type, Description
  * Usato da EventsGridView
- `FileNode.cs`: Rappresentazione file
- `FileSystemNode.cs`: Classe base nodi
- `MonitoredPaths.cs`: Gestione percorsi monitorati
- `NodeState.cs`: Stato monitoraggio
- `Statistics.cs`: Statistiche monitoraggio

### Core/Notifications/
- `NotificationManager.cs`: Gestione notifiche sistema

### Core/State/
- `NodeStateHandler.cs`: Gestione stati nodi
- `Selection/SelectionManager.cs`: Gestione selezioni UI

### Core/UI/
Interfaccia utente WPF:
- Components/
  * `DeleteConfirmationPopup.xaml/.cs`: Conferma eliminazione
  * `EventsGridView.xaml/.cs`: Visualizzazione eventi
    - Grid eventi con virtualizzazione
    - Colori condizionali per tipo evento
  * `MonitoredItemsView.xaml/.cs`: Vista elementi monitorati
  * `NewFilePopup.xaml/.cs`: Creazione nuovi file
  * `StatisticsPanel.xaml/.cs`: Statistiche monitoraggio

- Controls/
  * `IndependentTreeViewItem.cs`: Controllo tree personalizzato

- Converters/
  * `IconConverter.cs`: Conversione icone
  * `MonitoringStatusConverter.cs`: Conversione stati
  * `NullToBoolConverter.cs`: Conversione null

### Core/Utilities/
- `OperationHandler.cs`: Gestione operazioni sicure

### Core/ViewModels/
ViewModels MVVM:
- `FileSystemNodeViewModel.cs`: ViewModel nodi
- `StatisticsViewModel.cs`: ViewModel statistiche
- `TreeNodeViewModel.cs`: ViewModel base nodi
- `TreeViewModel.cs`: ViewModel principale
  * Coordina UI e logica
  * Gestisce collezione eventi
  * Integra FileSystemEventProcessor
- `TreeViewModelConfig.cs`: Configurazione

## Dipendenze Principali

1. Flusso Eventi Filesystem:
```
FileSystemMonitor -> FileSystemEventProcessor -> EventsGridView
                                            -> TreeViewModel
```

2. Gestione Stato:
```
TreeViewModel -> StateManager -> NodeStateHandler
             -> FileSystemNodeViewModel
```

3. UI Updates:
```
FileSystemEventProcessor -> TreeViewModel -> MonitoredItemsView
                                        -> EventsGridView
```

## Componenti Autonomi
- Sistema di logging (Core/Logging)
- Gestione notifiche (Core/Notifications)
- Convertitori UI (Core/UI/Converters)
- Utility generiche (Core/Utilities)

## Componenti Interconnessi
- FileSystem (monitoring, eventi, processamento)
- UI (views, viewmodels, stato)
- Gestione stato (StateManager, NodeStateHandler)

## Note Implementative
- Virtualizzazione UI per performance
- Gestione thread-safe degli eventi
- Persistenza stato automatica
- Logging integrato in tutti i componenti
- Sistema di notifiche centralizzato
