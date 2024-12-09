using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.State.Selection;
using FileGuard.Core.Cache;
using FileGuard.Core.FileSystem;
using FileGuard.Core.Notifications;

namespace FileGuard.Core.ViewModels
{
    public class TreeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IStateManager stateManager;
        private readonly ISelectionManager selectionManager;
        private readonly INotificationService notificationService;
        private readonly IChangeTracker changeTracker;
        private readonly IFileSystemManager fileSystemManager;
        private readonly TreeViewModelConfig config;
        private FileSystemNodeViewModel? selectedNode;
        private bool isDisposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ITreeNode> MonitoredNodes { get; }

        public ObservableCollection<FileChangedEventArgs> FileChanges => changeTracker.Changes;

        public FileSystemNodeViewModel? SelectedNode
        {
            get => selectedNode;
            set
            {
                if (selectedNode != value)
                {
                    selectedNode = value;
                    OnPropertyChanged(nameof(SelectedNode));
                }
            }
        }

        public TreeViewModel(TreeViewModelConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            
            var stateManagerConfig = new StateManagerConfig(config.SettingsPath);
            this.stateManager = new StateManager(stateManagerConfig);
            this.selectionManager = new SelectionManager(this.stateManager);
            this.notificationService = new NotificationManager();
            this.changeTracker = new ChangeTracker(Application.Current.Dispatcher, config.MaxChangeHistoryItems);
            
            // Inizializza FileSystemManager
            this.fileSystemManager = new FileSystemManager(
                config.SettingsPath,
                stateManager,
                selectionManager,
                Application.Current.Dispatcher
            );

            // Sottoscrivi agli eventi di filesystem
            this.fileSystemManager.FileSystemChanged += HandleFileSystemChanged;
            
            MonitoredNodes = new ObservableCollection<ITreeNode>();

            if (config.AutoLoadMonitoredFolders)
            {
                LoadMonitoredFolders();
            }
        }

        public void AddFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                Debug.WriteLine($"[Tree] Aggiunta cartella: {path}");
                var info = new DirectoryInfo(path);
                if (info.Exists)
                {
                    var node = new FileSystemNodeViewModel(path, info, stateManager);
                    MonitoredNodes.Add(node);
                    stateManager.AddMonitoredPath(path);
                    fileSystemManager.StartMonitoring(path);
                    SaveStateAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tree] Errore aggiunta cartella {path}: {ex.Message}");
                notificationService.ShowMessage(
                    $"Errore nell'aggiunta della cartella: {ex.Message}",
                    "Errore",
                    NotificationType.Error
                );
            }
        }

        public void RemoveFolder(ITreeNode? node)
        {
            if (node == null) return;

            try
            {
                Debug.WriteLine($"[Tree] Rimozione cartella: {node.Path}");
                MonitoredNodes.Remove(node);
                stateManager.RemoveMonitoredPath(node.Path);
                fileSystemManager.StopMonitoring(node.Path);
                SaveStateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tree] Errore rimozione cartella {node.Path}: {ex.Message}");
                notificationService.ShowMessage(
                    $"Errore nella rimozione della cartella: {ex.Message}",
                    "Errore",
                    NotificationType.Error
                );
            }
        }

        private void LoadMonitoredFolders()
        {
            try
            {
                Debug.WriteLine("[Tree] Caricamento cartelle monitorate");
                var paths = stateManager.GetMonitoredPaths();
                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        var info = new DirectoryInfo(path);
                        var node = new FileSystemNodeViewModel(path, info, stateManager);
                        MonitoredNodes.Add(node);
                        fileSystemManager.StartMonitoring(path);
                        Debug.WriteLine($"[Tree] Caricata cartella: {path}");
                    }
                    else
                    {
                        Debug.WriteLine($"[Tree] Cartella non trovata: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tree] Errore caricamento cartelle: {ex.Message}");
                notificationService.ShowMessage(
                    $"Errore nel caricamento delle cartelle: {ex.Message}",
                    "Errore",
                    NotificationType.Error
                );
            }
        }

        private void HandleFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            try
            {
                Debug.WriteLine($"[Tree] Cambiamento filesystem: {e.ChangeType} - {e.FullPath}");

                // Traccia il cambiamento
                var description = GetChangeDescription(e);
                changeTracker.TrackChange(e.FullPath, e.ChangeType.ToString(), description);

                // Trova e aggiorna il nodo appropriato
                var parentPath = Path.GetDirectoryName(e.FullPath);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var node = FindNode(parentPath);
                    if (node is FileSystemNodeViewModel fsNode)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            fsNode.LoadChildren(true); // Forza il ricaricamento
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tree] Errore gestione cambiamento filesystem: {ex.Message}");
            }
        }

        private string GetChangeDescription(FileSystemEventArgs e)
        {
            var fileName = Path.GetFileName(e.FullPath);
            return e.ChangeType switch
            {
                WatcherChangeTypes.Created => $"Nuovo elemento creato: {fileName}",
                WatcherChangeTypes.Deleted => $"Elemento eliminato: {fileName}",
                WatcherChangeTypes.Changed => $"Elemento modificato: {fileName}",
                WatcherChangeTypes.Renamed => e is RenamedEventArgs re ?
                    $"Elemento rinominato da {Path.GetFileName(re.OldFullPath)} a {fileName}" :
                    $"Elemento rinominato: {fileName}",
                _ => $"Cambiamento rilevato: {fileName}"
            };
        }

        private ITreeNode? FindNode(string path)
        {
            foreach (var node in MonitoredNodes)
            {
                if (node.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return node;

                if (node.IsDirectory)
                {
                    var found = FindNodeInChildren(node, path);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private ITreeNode? FindNodeInChildren(ITreeNode parent, string path)
        {
            foreach (var child in parent.Children)
            {
                if (child.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return child;

                if (child.IsDirectory)
                {
                    var found = FindNodeInChildren(child, path);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private async void SaveStateAsync()
        {
            try
            {
                await stateManager.SaveStateToDiskAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tree] Errore salvataggio stato: {ex.Message}");
                notificationService.ShowMessage(
                    $"Errore nel salvataggio dello stato: {ex.Message}",
                    "Errore",
                    NotificationType.Error
                );
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SaveState()
        {
            SaveStateAsync();
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                if (fileSystemManager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }
            }
            finally
            {
                isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
