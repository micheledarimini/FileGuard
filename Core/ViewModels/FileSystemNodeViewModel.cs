using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;

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
            this.fileSystemInfo = info;
            this.stateManager = stateManager;

            if (IsDirectory)
            {
                if (stateManager != null)
                {
                    var state = stateManager.GetOrCreateState(path);
                    if (state != null)
                    {
                        UpdateState(state.IsChecked, state.MonitoringStatus);
                        IsExpanded = state.IsExpanded;
                        
                        // Se il nodo era espanso, carica subito i figli
                        if (state.IsExpanded)
                        {
                            LoadChildrenFromState(state);
                        }
                        else
                        {
                            // Altrimenti aggiungi solo il dummy node
                            AddChild(new DummyNodeViewModel());
                        }
                    }
                    else
                    {
                        AddChild(new DummyNodeViewModel());
                    }
                }
                else
                {
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
        }

        private static bool IsDirectoryPath(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                return (attr & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch
            {
                return false;
            }
        }

        private void LoadChildrenFromState(NodeState state)
        {
            try
            {
                var entries = new DirectoryInfo(Path)
                    .EnumerateFileSystemInfos()
                    .OrderBy(info => info is not DirectoryInfo)
                    .ThenBy(info => info.Name);

                foreach (var entry in entries)
                {
                    var childPath = entry.FullName;
                    var childState = stateManager?.GetOrCreateState(childPath);
                    
                    var childNode = new FileSystemNodeViewModel(childPath, entry, stateManager, dispatcher);
                    
                    if (childState != null)
                    {
                        childNode.UpdateState(childState.IsChecked, childState.MonitoringStatus);
                        
                        // Se è una directory e ha figli salvati, imposta lo stato di espansione
                        if (childNode.IsDirectory && childState.ChildStates.Any())
                        {
                            childNode.IsExpanded = childState.IsExpanded;
                        }
                    }
                    else
                    {
                        childNode.UpdateState(IsChecked, MonitoringStatus);
                    }

                    AddChild(childNode);
                }

                isLoaded = true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ClearChildren();
                AddChild(new DummyNodeViewModel());
                isLoaded = false;
            }
        }

        public override void LoadChildren(bool forceReload = false)
        {
            if (!IsDirectory || (isLoaded && !forceReload)) return;

            try
            {
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

                // Fallback al caricamento standard se non c'è stato
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
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
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
        }

        public override void LoadChildren(bool forceReload = false)
        {
            // Non fa nulla perché è un nodo dummy
        }
    }
}
