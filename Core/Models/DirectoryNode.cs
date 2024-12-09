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
            selectionManager.SetNodeSelection(Path, state);
        }

        public void UpdateStateFromChildren()
        {
            // Non più necessario - gestito da SelectionManager
        }

        public void LoadContents(bool forceReload = false)
        {
            if (isLoaded && !forceReload) return;

            try
            {
                BeginSynchronizing();

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
                    }
                    else
                    {
                        node = new FileNode(childPath, dispatcher);
                    }

                    if (childState != null)
                    {
                        node.BeginSynchronizing();
                        node.SetStateDirectly(childState.IsChecked, childState.MonitoringStatus);
                        node.EndSynchronizing();

                        if (node is DirectoryNode dirNode)
                        {
                            dirNode.isExpanded = childState.IsExpanded;
                        }
                    }

                    node.Parent = this;
                    node.PropertyChanged += Child_PropertyChanged;
                    newChildren.Add(node);
                }

                Children = newChildren;
                isLoaded = true;

                // Carica lo stato dal SelectionManager
                selectionManager.LoadNodeState(Path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
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
                    selectionManager.SetNodeSelection(node.Path, node.IsChecked);
                }
            }
        }
    }
}
