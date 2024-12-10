using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;
using FileGuard.Core.FileSystem;
using FileGuard.Core.Cache;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;

namespace FileGuard.Core.ViewModels
{
    public class TreeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly TreeViewModelConfig _config;
        private readonly IFileSystemMonitor _monitor;
        private readonly IStateManager _stateManager;
        private ITreeNode? _selectedNode;
        private bool _isMainFolderSelected;
        private ObservableCollection<FileSystemNodeViewModel> _monitoredNodes;
        private ObservableCollection<FileChangeInfo> _fileChanges;
        private bool _isDisposed;

        public ObservableCollection<FileSystemNodeViewModel> MonitoredNodes => _monitoredNodes;
        public ObservableCollection<FileChangeInfo> FileChanges => _fileChanges;

        public ITreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnPropertyChanged();
                    UpdateIsMainFolderSelected();
                }
            }
        }

        public bool IsMainFolderSelected
        {
            get => _isMainFolderSelected;
            private set
            {
                if (_isMainFolderSelected != value)
                {
                    _isMainFolderSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateIsMainFolderSelected()
        {
            if (_selectedNode is FileSystemNodeViewModel node)
            {
                IsMainFolderSelected = node.IsDirectory && node.Parent == null;
            }
            else
            {
                IsMainFolderSelected = false;
            }
        }

        public TreeViewModel(TreeViewModelConfig config)
        {
            _config = config;
            _monitoredNodes = new ObservableCollection<FileSystemNodeViewModel>();
            _fileChanges = new ObservableCollection<FileChangeInfo>();
            _monitor = new FileSystemMonitor();
            _monitor.FileSystemChanged += OnFileSystemChanged;
            _stateManager = new StateManager(new StateManagerConfig(_config.SettingsPath));
            IsMainFolderSelected = false;
            LoadState();
        }

        public void AddFolder(string path)
        {
            try
            {
                Trace.WriteLine($"[TreeViewModel] Aggiunta cartella: {path}");
                var node = new FileSystemNodeViewModel(path, new DirectoryInfo(path), _stateManager);
                _monitoredNodes.Add(node);
                _monitor.StartMonitoring(path);
                SaveState();
                Trace.WriteLine($"[TreeViewModel] Cartella aggiunta con successo: {path}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[TreeViewModel] Errore aggiunta cartella {path}: {ex}");
                throw;
            }
        }

        public void RemoveFolder(ITreeNode node)
        {
            if (node is FileSystemNodeViewModel fsNode && fsNode.Parent == null)
            {
                try
                {
                    Trace.WriteLine($"[TreeViewModel] Rimozione cartella: {fsNode.Path}");
                    
                    // Ferma il monitoraggio
                    _monitor.StopMonitoring(fsNode.Path);
                    
                    // Rimuovi dalla UI
                    _monitoredNodes.Remove(fsNode);
                    
                    // Rimuovi lo stato
                    _stateManager.RemoveMonitoredPath(fsNode.Path);
                    
                    // Salva le modifiche
                    SaveState();
                    
                    Trace.WriteLine($"[TreeViewModel] Cartella rimossa con successo: {fsNode.Path}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[TreeViewModel] Errore rimozione cartella {fsNode.Path}: {ex}");
                    throw;
                }
            }
        }

        private void OnFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Aggiorna il nodo corrispondente nel tree
                UpdateNodeForPath(e.FullPath);

                // Aggiungi l'evento alla lista
                var changeInfo = new FileChangeInfo
                {
                    Timestamp = DateTime.Now,
                    Path = e.FullPath,
                    Type = e.ChangeType.ToString(),
                    Description = GetChangeDescription(e)
                };

                _fileChanges.Insert(0, changeInfo);

                // Mantieni solo gli ultimi 1000 eventi
                while (_fileChanges.Count > 1000)
                {
                    _fileChanges.RemoveAt(_fileChanges.Count - 1);
                }
            });
        }

        private void UpdateNodeForPath(string path)
        {
            foreach (var node in _monitoredNodes)
            {
                if (path.StartsWith(node.Path, StringComparison.OrdinalIgnoreCase))
                {
                    node.LoadChildren(true);
                    break;
                }
            }
        }

        private string GetChangeDescription(FileSystemEventArgs e)
        {
            return e.ChangeType switch
            {
                WatcherChangeTypes.Created => $"Creato nuovo {(Directory.Exists(e.FullPath) ? "cartella" : "file")}",
                WatcherChangeTypes.Deleted => "Elemento eliminato",
                WatcherChangeTypes.Changed => "Contenuto modificato",
                WatcherChangeTypes.Renamed => e is RenamedEventArgs re ? $"Rinominato da {Path.GetFileName(re.OldFullPath)}" : "Elemento rinominato",
                _ => "Modifica non specificata"
            };
        }

        public void SaveState()
        {
            try
            {
                Trace.WriteLine("[TreeViewModel] Inizio salvataggio stato");
                foreach (var node in _monitoredNodes)
                {
                    SaveNodeState(node);
                }
                (_stateManager as StateManager)?.SaveStateToDisk();
                Trace.WriteLine("[TreeViewModel] Stato salvato con successo");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[TreeViewModel] Errore salvataggio stato: {ex}");
                throw;
            }
        }

        private void SaveNodeState(FileSystemNodeViewModel node)
        {
            try
            {
                Trace.WriteLine($"[TreeViewModel] Salvataggio stato nodo: {node.Path}");
                _stateManager.UpdateNodeState(
                    node.Path,
                    node.MonitoringStatus,
                    node.IsChecked,
                    node.IsExpanded
                );

                foreach (var child in node.Children)
                {
                    if (child is FileSystemNodeViewModel childNode)
                    {
                        SaveNodeState(childNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[TreeViewModel] Errore salvataggio stato nodo {node.Path}: {ex}");
                throw;
            }
        }

        private void LoadState()
        {
            try
            {
                Trace.WriteLine("[TreeViewModel] Inizio caricamento stato");
                _monitoredNodes.Clear();
                foreach (var path in _stateManager.GetMonitoredPaths())
                {
                    if (Directory.Exists(path))
                    {
                        Trace.WriteLine($"[TreeViewModel] Caricamento cartella monitorata: {path}");
                        var node = new FileSystemNodeViewModel(path, new DirectoryInfo(path), _stateManager);
                        RestoreNodeState(node);
                        _monitoredNodes.Add(node);
                        _monitor.StartMonitoring(path);
                    }
                }
                Trace.WriteLine("[TreeViewModel] Stato caricato con successo");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[TreeViewModel] Errore caricamento stato: {ex}");
                throw;
            }
        }

        private void RestoreNodeState(FileSystemNodeViewModel node)
        {
            try
            {
                Trace.WriteLine($"[TreeViewModel] Ripristino stato nodo: {node.Path}");
                var state = _stateManager.GetOrCreateState(node.Path);
                if (state != null)
                {
                    node.UpdateState(state.IsChecked, state.MonitoringStatus);
                    node.IsExpanded = state.IsExpanded;
                }

                foreach (var child in node.Children)
                {
                    if (child is FileSystemNodeViewModel childNode)
                    {
                        RestoreNodeState(childNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[TreeViewModel] Errore ripristino stato nodo {node.Path}: {ex}");
                throw;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                SaveState();
                _monitor.Dispose();
                (_stateManager as IDisposable)?.Dispose();
                _isDisposed = true;
            }
        }
    }

    public class FileChangeInfo
    {
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
