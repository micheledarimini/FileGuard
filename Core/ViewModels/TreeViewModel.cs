using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;
using FileGuard.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FileGuard.Core.ViewModels
{
    public class TreeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly TreeViewModelConfig _config;
        private readonly IFileSystemMonitor _monitor;
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
            IsMainFolderSelected = false;
            LoadState();
        }

        public void AddFolder(string path)
        {
            var node = new FileSystemNodeViewModel(path);
            _monitoredNodes.Add(node);
            _monitor.StartMonitoring(path);
            SaveState();
        }

        public void RemoveFolder(ITreeNode node)
        {
            if (node is FileSystemNodeViewModel fsNode && fsNode.Parent == null)
            {
                _monitor.StopMonitoring(fsNode.Path);
                _monitoredNodes.Remove(fsNode);
                SaveState();
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
            // TODO: Implementare la logica di salvataggio
        }

        private void LoadState()
        {
            // TODO: Implementare la logica di caricamento
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
                _monitor.Dispose();
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
