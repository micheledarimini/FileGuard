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

        public DirectoryNode(string path, Dispatcher? dispatcher = null, DirectoryInfo? info = null, IStateManager? stateManager = null) 
            : base(path, dispatcher)
        {
            this.stateManager = stateManager;
            children = new ObservableCollection<FileSystemNode>();
            
            if (Directory.Exists(path))
            {
                children.Add(new DummyNode());
                
                if (stateManager != null)
                {
                    SyncWithState();
                }
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
            if (!isLoaded) return;
            
            foreach (var child in Children.Where(c => c is not DummyNode))
            {
                child.SetStateDirectly(state, state ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);
                
                if (child is DirectoryNode dirNode)
                {
                    dirNode.PropagateStateToChildren(state);
                }
            }

            if (stateManager != null)
            {
                stateManager.UpdateNodeState(Path, MonitoringStatus, IsChecked, isExpanded);
            }
        }

        public void UpdateStateFromChildren()
        {
            if (!isLoaded || Children.Count == 0) return;
            
            var realChildren = Children.Where(c => c is not DummyNode).ToList();
            if (!realChildren.Any()) return;

            var allChecked = realChildren.All(c => c.IsChecked == true);
            var allUnchecked = realChildren.All(c => c.IsChecked == false);

            if (allChecked)
            {
                SetStateDirectly(true, MonitoringStatus.FullyMonitored);
            }
            else if (allUnchecked)
            {
                SetStateDirectly(false, MonitoringStatus.NotMonitored);
            }
            else
            {
                SetStateDirectly(null, MonitoringStatus.PartiallyMonitored);
            }

            if (stateManager != null)
            {
                stateManager.UpdateNodeState(Path, MonitoringStatus, IsChecked, isExpanded);
            }

            (Parent as DirectoryNode)?.UpdateStateFromChildren();
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
                        node = new DirectoryNode(childPath, dispatcher, dirInfo, stateManager);
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
                    else 
                    {
                        node.BeginSynchronizing();
                        node.SetStateDirectly(IsChecked == true, IsChecked == true ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);
                        node.EndSynchronizing();
                    }

                    node.Parent = this;
                    node.PropertyChanged += Child_PropertyChanged;
                    newChildren.Add(node);
                }

                Children = newChildren;
                isLoaded = true;

                UpdateStateFromChildren();
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
                UpdateStateFromChildren();
            }
        }
    }
}
