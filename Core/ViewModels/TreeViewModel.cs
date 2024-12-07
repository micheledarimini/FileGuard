using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Diagnostics;
using FileGuard.Core.Models;
using FileGuard.Core.FileSystem;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Cache;

namespace FileGuard.Core.ViewModels
{
    public class TreeViewModel : ITreeViewModel, IDisposable
    {
        private readonly IFileSystemManager fileSystemManager;
        private readonly IStateManager stateManager;
        private readonly TreeViewModelConfig config;
        private ObservableCollection<FileChangedEventArgs> fileChanges;
        private FileSystemNode? selectedNode;
        private bool isDisposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<FileSystemNode> MonitoredNodes => fileSystemManager.MonitoredNodes;

        public ObservableCollection<FileChangedEventArgs> FileChanges
        {
            get => fileChanges;
            private set
            {
                fileChanges = value;
                OnPropertyChanged(nameof(FileChanges));
            }
        }

        public FileSystemNode? SelectedNode
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
            this.fileSystemManager = new FileSystemManager(
                config.SettingsPath,
                this.stateManager,
                Application.Current.Dispatcher
            );
            
            this.fileChanges = new ObservableCollection<FileChangedEventArgs>();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            fileSystemManager.FileSystemChanged += HandleFileSystemChanged;
            MonitoredNodes.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (FileSystemNode node in e.NewItems)
                    {
                        SubscribeToNodeChanges(node);
                    }
                }
            };

            if (config.AutoLoadMonitoredFolders)
            {
                LoadMonitoredFolders();
            }
        }

        private void SubscribeToNodeChanges(FileSystemNode node)
        {
            node.PropertyChanged += HandleNodePropertyChanged;

            if (node is DirectoryNode dirNode)
            {
                dirNode.Children.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (FileSystemNode child in e.NewItems)
                        {
                            SubscribeToNodeChanges(child);
                        }
                    }
                };
            }
        }

        private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is FileSystemNode node && e.PropertyName == nameof(FileSystemNode.IsChecked))
            {
                stateManager.UpdateNodeState(
                    node.Path,
                    node.MonitoringStatus,
                    node.IsChecked,
                    node is DirectoryNode dirNode ? dirNode.IsExpanded : false
                );
                SaveStateAsync();
            }
        }

        private void HandleFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var args = new FileChangedEventArgs(
                    e.FullPath,
                    e.ChangeType.ToString(),
                    GetChangeDescription(e)
                );
                FileChanges.Insert(0, args);

                while (FileChanges.Count > config.MaxChangeHistoryItems)
                {
                    FileChanges.RemoveAt(FileChanges.Count - 1);
                }
            }));
        }

        public void AddFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                fileSystemManager.StartMonitoring(path);
                stateManager.AddMonitoredPath(path);
                SaveStateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in AddFolder: {ex.Message}");
                MessageBox.Show($"Errore nell'aggiunta della cartella: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveFolder(FileSystemNode? node)
        {
            if (node == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Sei sicuro di voler rimuovere {node.DisplayName} dal monitoraggio?",
                    "Conferma rimozione",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    fileSystemManager.StopMonitoring(node.Path);
                    stateManager.RemoveMonitoredPath(node.Path);
                    SaveStateAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in RemoveFolder: {ex.Message}");
                MessageBox.Show($"Errore nella rimozione della cartella: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateNodeState(string path, bool isExpanded)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var node = FindNode(path);
                if (node is DirectoryNode dirNode)
                {
                    dirNode.IsExpanded = isExpanded;
                    stateManager.UpdateIsExpanded(path, isExpanded);
                    SaveStateAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in UpdateNodeState: {ex.Message}");
                MessageBox.Show($"Errore nell'aggiornamento dello stato: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMonitoredFolders()
        {
            var paths = stateManager.GetMonitoredPaths();
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    fileSystemManager.StartMonitoring(path);
                }
            }
        }

        private FileSystemNode? FindNode(string path)
        {
            foreach (var node in MonitoredNodes)
            {
                if (node.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                if (node is DirectoryNode dirNode)
                {
                    var result = FindNodeInChildren(dirNode, path);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private FileSystemNode? FindNodeInChildren(DirectoryNode parent, string path)
        {
            foreach (var child in parent.Children)
            {
                if (child.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                if (child is DirectoryNode dirNode)
                {
                    var result = FindNodeInChildren(dirNode, path);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
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

        private async void SaveStateAsync()
        {
            try
            {
                await stateManager.SaveStateToDiskAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in SaveStateAsync: {ex.Message}");
                MessageBox.Show($"Errore nel salvataggio dello stato: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SaveState()
        {
            if (isDisposed) return;
            SaveStateAsync();
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                SaveStateAsync();
                
                if (fileSystemManager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }

                if (stateManager is IDisposable disposableState)
                {
                    disposableState.Dispose();
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
