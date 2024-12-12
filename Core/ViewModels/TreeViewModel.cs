using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using FileGuard.Core.Cache;
using FileGuard.Core.Cache.Config;
using FileGuard.Core.FileSystem;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Logging;
using FileGuard.Core.Models;
using FileGuard.Core.Utilities;

namespace FileGuard.Core.ViewModels
{
    public class TreeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly TreeViewModelConfig _config;
        private readonly IFileSystemMonitor _monitor;
        private readonly IStateManager _stateManager;
        private readonly ILogger _logger;
        private readonly FileSystemEventProcessor _eventProcessor;
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

        public TreeViewModel(TreeViewModelConfig config)
        {
            _config = config;
            _monitoredNodes = new ObservableCollection<FileSystemNodeViewModel>();
            _fileChanges = new ObservableCollection<FileChangeInfo>();
            _monitor = new FileSystemMonitor();
            _stateManager = new StateManager(new StateManagerConfig(_config.SettingsPath));
            _logger = LoggerFactory.GetDefaultLogger();
            _eventProcessor = new FileSystemEventProcessor(_fileChanges, _logger);

            _monitor.FileSystemChanged += OnFileSystemChanged;
            IsMainFolderSelected = false;

            LoadState();
        }

        public void AddFolder(string path)
        {
            OperationHandler.ExecuteSafe(() =>
            {
                var node = new FileSystemNodeViewModel(path, new DirectoryInfo(path), _stateManager);
                _monitoredNodes.Add(node);
                _monitor.StartMonitoring(path);
                SaveState();
            }, _logger, nameof(TreeViewModel), "AddFolder");
        }

        public void RemoveFolder(ITreeNode node)
        {
            if (node is FileSystemNodeViewModel fsNode && fsNode.Parent == null)
            {
                OperationHandler.ExecuteSafe(() =>
                {
                    _monitor.StopMonitoring(fsNode.Path);
                    _monitoredNodes.Remove(fsNode);
                    _stateManager.RemoveMonitoredPath(fsNode.Path);
                    SaveState();
                }, _logger, nameof(TreeViewModel), "RemoveFolder");
            }
        }

        private void OnFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            _eventProcessor.ProcessChange(e, path => _eventProcessor.UpdateNodes(path, _monitoredNodes));
        }

        public void SaveState()
        {
            OperationHandler.ExecuteSafe(() =>
            {
                foreach (var node in _monitoredNodes)
                {
                    SaveNodeState(node);
                }
                (_stateManager as StateManager)?.SaveStateToDisk();
            }, _logger, nameof(TreeViewModel), "SaveState");
        }

        private void SaveNodeState(FileSystemNodeViewModel node)
        {
            OperationHandler.ExecuteSafe(() =>
            {
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
            }, _logger, nameof(TreeViewModel), "SaveNodeState");
        }

        private void LoadState()
        {
            OperationHandler.ExecuteSafe(() =>
            {
                _monitoredNodes.Clear();
                foreach (var path in _stateManager.GetMonitoredPaths())
                {
                    if (Directory.Exists(path))
                    {
                        var node = new FileSystemNodeViewModel(path, new DirectoryInfo(path), _stateManager);
                        RestoreNodeState(node);
                        _monitoredNodes.Add(node);
                        _monitor.StartMonitoring(path);
                    }
                }
            }, _logger, nameof(TreeViewModel), "LoadState");
        }

        private void RestoreNodeState(FileSystemNodeViewModel node)
        {
            OperationHandler.ExecuteSafe(() =>
            {
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
            }, _logger, nameof(TreeViewModel), "RestoreNodeState");
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                OperationHandler.ExecuteSafe(() =>
                {
                    SaveState();
                    _monitor.Dispose();
                    (_stateManager as IDisposable)?.Dispose();
                }, _logger, nameof(TreeViewModel), "Dispose");

                _isDisposed = true;
            }
        }
    }
}
