using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;

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
                // Aggiungi un nodo dummy per mostrare l'espansione
                AddChild(new DummyNodeViewModel());

                if (stateManager != null)
                {
                    var state = stateManager.GetOrCreateState(path);
                    if (state != null)
                    {
                        UpdateState(state.IsChecked, state.MonitoringStatus);
                        IsExpanded = state.IsExpanded;
                    }
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

        public override void LoadChildren(bool forceReload = false)
        {
            if (!IsDirectory || (isLoaded && !forceReload)) return;

            try
            {
                var wasExpanded = IsExpanded;
                ClearChildren();

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
                        if (childNode.IsDirectory)
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
