using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;

using FileGuard.Core.Interfaces;

namespace FileGuard.Core.Models
{
    public class DirectoryNode : FileSystemNode
    {
        private readonly IStateManager? stateManager;
        private readonly ISelectionManager selectionManager;
        private readonly object syncLock = new object();
        private ObservableCollection<FileSystemNode> children;
        private bool isExpanded;
        private bool isLoaded;

        public override bool IsDirectory => true;

        public ObservableCollection<FileSystemNode> Children
        {
            get => children;
            private set
            {
                children = value;
                OnPropertyChanged(nameof(Children));
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                    
                    if (value && !isLoaded)
                    {
                        LoadContents();
                    }

                    if (operationMode == NodeOperationMode.Interactive && stateManager != null)
                    {
                        Trace.WriteLine($"[DirectoryNode] IsExpanded cambiato: {Path} => {value}");
                        stateManager.UpdateNodeState(Path, MonitoringStatus, IsChecked, isExpanded);
                    }
                }
            }
        }

        public DirectoryNode(string path, Dispatcher? dispatcher = null, DirectoryInfo? info = null, IStateManager? stateManager = null, ISelectionManager? selectionManager = null) 
            : base(path, dispatcher)
        {
            this.stateManager = stateManager;
            this.selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));
            children = new ObservableCollection<FileSystemNode>();
            
            if (Directory.Exists(path))
            {
                children.Add(new DummyNode());
                
                if (stateManager != null)
                {
                    Trace.WriteLine($"[DirectoryNode] Costruttore: {path}");
                    SyncWithState();
                }
            }

            // Sottoscrizione agli eventi di selezione
            this.selectionManager.SelectionChanged += HandleSelectionChanged;
        }

        private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.Path.Equals(Path, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine($"[DirectoryNode] SelectionChanged: {Path} => IsChecked: {e.IsChecked}, Status: {e.Status}");
                SetStateDirectly(e.IsChecked, e.Status);
            }
        }

        private void SyncWithState()
        {
            BeginSynchronizing();
            try
            {
                var state = stateManager?.GetOrCreateState(Path);
                if (state != null)
                {
                    Trace.WriteLine($"[DirectoryNode] SyncWithState: {Path} => IsChecked: {state.IsChecked}, Status: {state.MonitoringStatus}, IsExpanded: {state.IsExpanded}, ChildCount: {state.ChildStates.Count}");
                    SetStateDirectly(state.IsChecked, state.MonitoringStatus);
                    isExpanded = state.IsExpanded;
                }
                
                if (isExpanded)
                {
                    LoadContents();
                }
            }
            finally
            {
                EndSynchronizing();
            }
        }

        protected override void PropagateStateToChildren(bool state)
        {
            Trace.WriteLine($"[DirectoryNode] PropagateStateToChildren: {Path} => {state}");
            selectionManager.SetNodeSelection(Path, state);
        }

        public void LoadContents(bool forceReload = false)
        {
            if (isLoaded && !forceReload) return;

            try
            {
                BeginSynchronizing();
                Trace.WriteLine($"[DirectoryNode] LoadContents iniziato: {Path}");

                // Carica prima lo stato dal SelectionManager
                selectionManager.LoadNodeState(Path);

                var entries = new DirectoryInfo(Path)
                    .EnumerateFileSystemInfos()
                    .OrderBy(info => info is not DirectoryInfo)
                    .ThenBy(info => info.Name);

                var newChildren = new ObservableCollection<FileSystemNode>();

                foreach (var entry in entries)
                {
                    var childPath = entry.FullName;
                    var childState = stateManager?.GetOrCreateState(childPath);

                    FileSystemNode node;
                    if (entry is DirectoryInfo dirInfo)
                    {
                        node = new DirectoryNode(childPath, dispatcher, dirInfo, stateManager, selectionManager);
                        Trace.WriteLine($"[DirectoryNode] LoadContents - Creato nodo directory: {childPath}");
                    }
                    else
                    {
                        node = new FileNode(childPath, dispatcher, entry as FileInfo, stateManager, selectionManager);
                        Trace.WriteLine($"[DirectoryNode] LoadContents - Creato nodo file: {childPath}");
                    }

                    if (childState != null)
                    {
                        Trace.WriteLine($"[DirectoryNode] LoadContents - Child state trovato: {childPath} => IsChecked: {childState.IsChecked}, Status: {childState.MonitoringStatus}, IsExpanded: {childState.IsExpanded}");
                        node.BeginSynchronizing();
                        node.SetStateDirectly(childState.IsChecked, childState.MonitoringStatus);
                        node.EndSynchronizing();

                        if (node is DirectoryNode dirNode)
                        {
                            dirNode.isExpanded = childState.IsExpanded;
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"[DirectoryNode] LoadContents - Child state non trovato: {childPath}");
                    }

                    node.Parent = this;
                    node.PropertyChanged += Child_PropertyChanged;
                    newChildren.Add(node);
                }

                Children = newChildren;
                isLoaded = true;

                Trace.WriteLine($"[DirectoryNode] LoadContents completato: {Path} => {newChildren.Count} figli caricati");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Trace.WriteLine($"[DirectoryNode] LoadContents errore: {Path} => {ex.Message}");
                Children = new ObservableCollection<FileSystemNode>();
                children.Add(new DummyNode());
                isLoaded = false;
            }
            finally
            {
                EndSynchronizing();
            }
        }

        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(IsChecked) || e.PropertyName == nameof(MonitoringStatus)) 
                && operationMode == NodeOperationMode.Interactive)
            {
                if (sender is FileSystemNode node)
                {
                    Trace.WriteLine($"[DirectoryNode] Child_PropertyChanged: {node.Path} => IsChecked: {node.IsChecked}, Status: {node.MonitoringStatus}");
                    selectionManager.SetNodeSelection(node.Path, node.IsChecked);
                }
            }
        }
    }
}
