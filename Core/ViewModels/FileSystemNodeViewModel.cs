using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.Logging;

namespace FileGuard.Core.ViewModels
{
    public class FileSystemNodeViewModel : TreeNodeViewModel
    {
        private readonly FileSystemInfo fileSystemInfo;
        private readonly IStateManager? stateManager;
        private bool isLoaded;

        public override string DisplayName => fileSystemInfo.Name;
        public override bool IsDirectory => fileSystemInfo is DirectoryInfo;

        public FileSystemNodeViewModel(string path, FileSystemInfo info, IStateManager? stateManager = null, Dispatcher? dispatcher = null) 
            : base(path, dispatcher)
        {
            Trace.WriteLine($"[FileSystemNodeViewModel] Creazione nodo: {path}");
            logger.LogDebug($"Creazione nodo file system: {path}, Tipo: {(info is DirectoryInfo ? "Directory" : "File")}", nameof(FileSystemNodeViewModel));
            
            this.fileSystemInfo = info;
            this.stateManager = stateManager;

            if (IsDirectory)
            {
                if (stateManager != null)
                {
                    Trace.WriteLine($"[FileSystemNodeViewModel] Caricamento stato per: {path}");
                    logger.LogDebug($"Caricamento stato per directory: {path}", nameof(FileSystemNodeViewModel));

                    var state = stateManager.GetOrCreateState(path);
                    if (state != null)
                    {
                        Trace.WriteLine($"[FileSystemNodeViewModel] Stato trovato per {path}: IsChecked={state.IsChecked}, Status={state.MonitoringStatus}, IsExpanded={state.IsExpanded}");
                        logger.LogDebug($"Stato trovato per {path}: IsChecked={state.IsChecked}, Status={state.MonitoringStatus}, IsExpanded={state.IsExpanded}", nameof(FileSystemNodeViewModel));

                        UpdateState(state.IsChecked, state.MonitoringStatus);
                        IsExpanded = state.IsExpanded;

                        if (state.IsExpanded)
                        {
                            Trace.WriteLine($"[FileSystemNodeViewModel] Caricamento figli per nodo espanso: {path}");
                            logger.LogDebug($"Caricamento figli per nodo espanso: {path}", nameof(FileSystemNodeViewModel));
                            LoadChildrenFromState(state);
                        }
                        else
                        {
                            Trace.WriteLine($"[FileSystemNodeViewModel] Aggiunto dummy node per: {path}");
                            logger.LogDebug($"Aggiunto dummy node per directory non espansa: {path}", nameof(FileSystemNodeViewModel));
                            AddChild(new DummyNodeViewModel());
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"[FileSystemNodeViewModel] Nessuno stato trovato per: {path}, aggiunto dummy node");
                        logger.LogDebug($"Nessuno stato trovato, aggiunto dummy node: {path}", nameof(FileSystemNodeViewModel));
                        AddChild(new DummyNodeViewModel());
                    }
                }
                else
                {
                    Trace.WriteLine($"[FileSystemNodeViewModel] Nessun state manager per: {path}, aggiunto dummy node");
                    logger.LogDebug($"Nessun state manager disponibile, aggiunto dummy node: {path}", nameof(FileSystemNodeViewModel));
                    AddChild(new DummyNodeViewModel());
                }
            }
        }

        public FileSystemNodeViewModel(string path, Dispatcher? dispatcher = null)
            : this(path, 
                  IsDirectoryPath(path) ? new DirectoryInfo(path) : new FileInfo(path) as FileSystemInfo, 
                  null, 
                  dispatcher)
        {
            Trace.WriteLine($"[FileSystemNodeViewModel] Creazione nodo senza state manager: {path}");
            logger.LogDebug($"Creazione nodo senza state manager: {path}", nameof(FileSystemNodeViewModel));
        }

        private static bool IsDirectoryPath(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                return (attr & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Errore verifica directory: {path}, {ex.Message}");
                logger.LogError($"Errore verifica directory: {path}", ex, nameof(FileSystemNodeViewModel));
                return false;
            }
        }

        private void LoadChildrenFromState(NodeState state)
        {
            try
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Inizio caricamento figli per: {Path}");
                logger.LogDebug($"Inizio caricamento figli da stato per: {Path}", nameof(FileSystemNodeViewModel));

                var entries = new DirectoryInfo(Path)
                    .EnumerateFileSystemInfos()
                    .OrderBy(info => info is not DirectoryInfo)
                    .ThenBy(info => info.Name);

                foreach (var entry in entries)
                {
                    var childPath = entry.FullName;
                    var childState = stateManager?.GetOrCreateState(childPath);

                    Trace.WriteLine($"[FileSystemNodeViewModel] Creazione nodo figlio: {childPath}");
                    logger.LogDebug($"Creazione nodo figlio: {childPath}, Tipo: {(entry is DirectoryInfo ? "Directory" : "File")}", nameof(FileSystemNodeViewModel));

                    var childNode = new FileSystemNodeViewModel(childPath, entry, stateManager, dispatcher);

                    if (childState != null)
                    {
                        Trace.WriteLine($"[FileSystemNodeViewModel] Stato trovato per figlio {childPath}: IsChecked={childState.IsChecked}, Status={childState.MonitoringStatus}");
                        logger.LogDebug($"Stato trovato per figlio {childPath}: IsChecked={childState.IsChecked}, Status={childState.MonitoringStatus}", nameof(FileSystemNodeViewModel));

                        childNode.UpdateState(childState.IsChecked, childState.MonitoringStatus);

                        if (childNode.IsDirectory && childState.ChildStates.Any())
                        {
                            childNode.IsExpanded = childState.IsExpanded;
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"[FileSystemNodeViewModel] Nessuno stato trovato per figlio {childPath}, ereditato da padre");
                        logger.LogDebug($"Nessuno stato trovato per figlio {childPath}, stato ereditato da padre", nameof(FileSystemNodeViewModel));
                        childNode.UpdateState(IsChecked, MonitoringStatus);
                    }

                    AddChild(childNode);
                }

                isLoaded = true;
                Trace.WriteLine($"[FileSystemNodeViewModel] Completato caricamento figli per: {Path}");
                logger.LogDebug($"Completato caricamento figli per: {Path}, totale figli: {Children.Count}", nameof(FileSystemNodeViewModel));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Errore caricamento figli per {Path}: {ex.Message}");
                logger.LogError($"Errore caricamento figli per {Path}", ex, nameof(FileSystemNodeViewModel));
                ClearChildren();
                AddChild(new DummyNodeViewModel());
                isLoaded = false;
            }
        }

        public override void LoadChildren(bool forceReload = false)
        {
            if (!IsDirectory || (isLoaded && !forceReload))
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Skip caricamento figli per: {Path}, IsDirectory={IsDirectory}, isLoaded={isLoaded}, forceReload={forceReload}");
                logger.LogDebug($"Skip caricamento figli: {Path}, IsDirectory={IsDirectory}, isLoaded={isLoaded}, forceReload={forceReload}", nameof(FileSystemNodeViewModel));
                return;
            }

            try
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Inizio LoadChildren per: {Path}");
                logger.LogDebug($"Inizio LoadChildren per: {Path}, forceReload={forceReload}", nameof(FileSystemNodeViewModel));

                var wasExpanded = IsExpanded;
                ClearChildren();

                if (stateManager != null)
                {
                    var state = stateManager.GetOrCreateState(Path);
                    if (state != null)
                    {
                        LoadChildrenFromState(state);
                        IsExpanded = wasExpanded;
                        return;
                    }
                }

                Trace.WriteLine($"[FileSystemNodeViewModel] Caricamento standard per: {Path}");
                logger.LogDebug($"Caricamento standard per: {Path}", nameof(FileSystemNodeViewModel));

                var entries = new DirectoryInfo(Path)
                    .EnumerateFileSystemInfos()
                    .OrderBy(info => info is not DirectoryInfo)
                    .ThenBy(info => info.Name);

                foreach (var entry in entries)
                {
                    var childNode = new FileSystemNodeViewModel(entry.FullName, entry, stateManager, dispatcher);
                    childNode.UpdateState(IsChecked, MonitoringStatus);
                    AddChild(childNode);
                }

                isLoaded = true;
                IsExpanded = wasExpanded;
                Trace.WriteLine($"[FileSystemNodeViewModel] Completato LoadChildren per: {Path}");
                logger.LogDebug($"Completato LoadChildren per: {Path}, totale figli: {Children.Count}", nameof(FileSystemNodeViewModel));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Errore in LoadChildren per {Path}: {ex.Message}");
                logger.LogError($"Errore in LoadChildren per {Path}", ex, nameof(FileSystemNodeViewModel));
                ClearChildren();
                AddChild(new DummyNodeViewModel());
                isLoaded = false;
            }
        }

        protected override void OnStateChanged(TreeNodeStateChangedEventArgs e)
        {
            base.OnStateChanged(e);

            if (stateManager != null)
            {
                Trace.WriteLine($"[FileSystemNodeViewModel] Aggiornamento stato per {Path}: Status={e.Status}, IsChecked={e.IsChecked}");
                logger.LogDebug($"Aggiornamento stato per {Path}: Status={e.Status}, IsChecked={e.IsChecked}", nameof(FileSystemNodeViewModel));
                stateManager.UpdateNodeState(Path, MonitoringStatus, IsChecked, IsExpanded);
            }
        }
    }

    public class DummyNodeViewModel : TreeNodeViewModel
    {
        public override string DisplayName => "Caricamento...";
        public override bool IsDirectory => false;

        public DummyNodeViewModel(Dispatcher? dispatcher = null) 
            : base(string.Empty, dispatcher)
        {
            Trace.WriteLine("[DummyNodeViewModel] Creazione nodo dummy");
            logger.LogDebug("Creazione nodo dummy", nameof(DummyNodeViewModel));
        }

        public override void LoadChildren(bool forceReload = false)
        {
            Trace.WriteLine("[DummyNodeViewModel] Tentativo di caricamento figli su nodo dummy ignorato");
            logger.LogDebug("Tentativo di caricamento figli su nodo dummy ignorato", nameof(DummyNodeViewModel));
        }
    }
}
