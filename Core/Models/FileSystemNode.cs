using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows;
using System.Linq;

namespace FileGuard.Core.Models
{
    public enum MonitoringStatus
    {
        NotMonitored,
        PartiallyMonitored,
        FullyMonitored
    }

    public enum NodeOperationMode
    {
        Synchronizing,
        Interactive
    }

    public abstract class FileSystemNode : INotifyPropertyChanged
    {
        private string path;
        private DateTime lastModified;
        protected bool? isChecked;
        protected MonitoringStatus monitoringStatus;
        protected readonly Dispatcher? dispatcher;
        protected FileSystemNode? parent;
        protected NodeOperationMode operationMode;
        private bool isUpdatingState;

        public event PropertyChangedEventHandler? PropertyChanged;

        public FileSystemNode? Parent
        {
            get => parent;
            set
            {
                if (parent != value)
                {
                    parent = value;
                    OnPropertyChanged(nameof(Parent));
                }
            }
        }

        public string Path
        {
            get => path;
            protected set
            {
                if (path != value)
                {
                    path = value;
                    OnPropertyChanged(nameof(Path));
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string Name => System.IO.Path.GetFileName(Path);

        public virtual string DisplayName => Name;

        public DateTime LastModified
        {
            get => lastModified;
            protected set
            {
                if (lastModified != value)
                {
                    lastModified = value;
                    OnPropertyChanged(nameof(LastModified));
                }
            }
        }

        public bool? IsChecked
        {
            get => isChecked;
            set
            {
                if (!isUpdatingState)
                {
                    try
                    {
                        isUpdatingState = true;

                        if (IsDirectory)
                        {
                            bool newState = !(isChecked == true);
                            SetStateWithoutPropagation(newState);
                            PropagateStateToChildren(newState);
                        }
                        else
                        {
                            SetStateWithoutPropagation(value == true);
                            if (Parent is DirectoryNode dirParent)
                            {
                                dirParent.PropagateStateToChildren(value == true);
                            }
                        }
                    }
                    finally
                    {
                        isUpdatingState = false;
                    }
                }
            }
        }

        public MonitoringStatus MonitoringStatus
        {
            get => monitoringStatus;
            set
            {
                if (!isUpdatingState && monitoringStatus != value)
                {
                    try
                    {
                        isUpdatingState = true;
                        monitoringStatus = value;
                        OnPropertyChanged(nameof(MonitoringStatus));
                    }
                    finally
                    {
                        isUpdatingState = false;
                    }
                }
            }
        }

        public abstract bool IsDirectory { get; }

        protected FileSystemNode(string path, Dispatcher? dispatcher = null)
        {
            this.path = path;
            this.dispatcher = dispatcher ?? (Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);
            this.lastModified = GetLastModifiedTime();
            this.isChecked = false;
            this.monitoringStatus = MonitoringStatus.NotMonitored;
            this.parent = null;
            this.operationMode = NodeOperationMode.Interactive;
            this.isUpdatingState = false;
        }

        protected void SetStateWithoutPropagation(bool? state)
        {
            if (isChecked != state)
            {
                isChecked = state;
                monitoringStatus = state == true ? MonitoringStatus.FullyMonitored :
                                 state == false ? MonitoringStatus.NotMonitored :
                                 MonitoringStatus.PartiallyMonitored;
                
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(MonitoringStatus));
            }
        }

        protected virtual void PropagateStateToChildren(bool state)
        {
            // Override in DirectoryNode
        }

        public void BeginSynchronizing()
        {
            operationMode = NodeOperationMode.Synchronizing;
        }

        public void EndSynchronizing()
        {
            operationMode = NodeOperationMode.Interactive;
        }

        public void SetStateDirectly(bool? isChecked, MonitoringStatus status)
        {
            try
            {
                isUpdatingState = true;
                this.isChecked = isChecked;
                this.monitoringStatus = status;
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(MonitoringStatus));
            }
            finally
            {
                isUpdatingState = false;
            }
        }

        public virtual void UpdateMonitoringStatus()
        {
            OnPropertyChanged(nameof(MonitoringStatus));
        }

        public void UpdateLastModified()
        {
            LastModified = GetLastModifiedTime();
        }

        public void UpdatePath(string newPath)
        {
            if (Path != newPath)
            {
                Path = newPath;
                LastModified = GetLastModifiedTime();
            }
        }

        protected virtual DateTime GetLastModifiedTime()
        {
            try
            {
                if (IsDirectory)
                {
                    return Directory.Exists(Path) ? Directory.GetLastWriteTime(Path) : DateTime.MinValue;
                }
                return File.Exists(Path) ? File.GetLastWriteTime(Path) : DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in GetLastModifiedTime per {Path}: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            try
            {
                if (dispatcher?.CheckAccess() == false)
                {
                    dispatcher.Invoke(() => OnPropertyChanged(propertyName));
                    return;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore in OnPropertyChanged per {Path}: {ex.Message}");
            }
        }

        public override string ToString() => Path;

        public override bool Equals(object? obj)
        {
            if (obj is FileSystemNode other)
            {
                return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Path.ToLowerInvariant().GetHashCode();
        }
    }
}
