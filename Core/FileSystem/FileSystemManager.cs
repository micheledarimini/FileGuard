using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemManager : IFileSystemManager, IDisposable
    {
        private readonly string settingsPath;
        private readonly IStateManager stateManager;
        private readonly ISelectionManager selectionManager;
        private readonly FileSystemNodeFactory nodeFactory;
        private readonly ObservableCollection<FileSystemNode> monitoredNodes;
        private readonly Dispatcher dispatcher;
        private readonly ConcurrentDictionary<string, FileSystemWatcher> watchers;
        private bool isDisposed;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<FileSystemEventArgs>? FileSystemChanged;

        public ObservableCollection<FileSystemNode> MonitoredNodes => monitoredNodes;

        public FileSystemManager(string settingsPath, IStateManager stateManager, ISelectionManager selectionManager, Dispatcher? dispatcher = null)
        {
            Debug.WriteLine("FileSystemManager: Inizializzazione");
            this.settingsPath = settingsPath;
            this.stateManager = stateManager;
            this.selectionManager = selectionManager;
            this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            this.monitoredNodes = new ObservableCollection<FileSystemNode>();
            this.watchers = new ConcurrentDictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

            Debug.WriteLine($"FileSystemManager: Creazione NodeFactory con settingsPath = {settingsPath}");
            this.nodeFactory = new FileSystemNodeFactory(this.dispatcher, this.stateManager, this.selectionManager);
            Debug.WriteLine("FileSystemManager: Inizializzazione completata");
        }

        public void StartMonitoring(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            Debug.WriteLine($"FileSystemManager: StartMonitoring per path = {path}");

            try
            {
                var node = FindNode(path);
                if (node == null)
                {
                    Debug.WriteLine($"FileSystemManager: Ricerca nodo per path = {path}");
                    var info = new DirectoryInfo(path);
                    if (info.Exists)
                    {
                        Debug.WriteLine($"FileSystemManager: Nodo non trovato = {path}");
                        node = nodeFactory.CreateNode(path, info);
                        monitoredNodes.Add(node);
                        Debug.WriteLine($"FileSystemManager: Aggiunto nuovo nodo = {path}");
                        OnPropertyChanged(nameof(MonitoredNodes));

                        // Crea e avvia il watcher per questa directory
                        CreateWatcher(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileSystemManager: Errore in StartMonitoring per {path}: {ex}");
                throw;
            }
        }

        public void StopMonitoring(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var node = FindNode(path);
            if (node != null)
            {
                monitoredNodes.Remove(node);
                OnPropertyChanged(nameof(MonitoredNodes));

                // Rimuovi e disponi il watcher
                if (watchers.TryRemove(path, out var watcher))
                {
                    watcher.Dispose();
                }
            }
        }

        private void CreateWatcher(string path)
        {
            try
            {
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName | 
                                 NotifyFilters.DirectoryName |
                                 NotifyFilters.LastWrite
                };

                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Changed += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemEvent;
                watcher.Error += OnWatcherError;

                if (watchers.TryAdd(path, watcher))
                {
                    Debug.WriteLine($"FileSystemManager: Watcher creato per {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileSystemManager: Errore creazione watcher per {path}: {ex}");
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    Debug.WriteLine($"FileSystemManager: Evento filesystem - {e.ChangeType}: {e.FullPath}");
                    OnFileSystemChanged(e);

                    // Aggiorna il nodo appropriato
                    var parentPath = Path.GetDirectoryName(e.FullPath);
                    var parentNode = FindNode(parentPath ?? string.Empty);
                    
                    if (parentNode is DirectoryNode dirNode)
                    {
                        dirNode.LoadContents(true);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileSystemManager: Errore gestione evento per {e.FullPath}: {ex}");
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine($"FileSystemManager: Errore watcher - {e.GetException()}");
            
            if (sender is FileSystemWatcher watcher)
            {
                try
                {
                    // Riavvia il watcher
                    watcher.EnableRaisingEvents = false;
                    watcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FileSystemManager: Errore riavvio watcher: {ex}");
                }
            }
        }

        private FileSystemNode? FindNode(string path)
        {
            foreach (var node in monitoredNodes)
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnFileSystemChanged(FileSystemEventArgs e)
        {
            FileSystemChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                // Disponi tutti i watchers
                foreach (var watcher in watchers.Values)
                {
                    watcher.Dispose();
                }
                watchers.Clear();
                monitoredNodes.Clear();
            }
            finally
            {
                isDisposed = true;
            }
        }
    }
}
