using System;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface ISelectionManager
    {
        void SetNodeSelection(string path, bool? state);
        bool? GetNodeSelection(string path);
        MonitoringStatus GetMonitoringStatus(string path);
        void LoadNodeState(string path);
        event EventHandler<SelectionChangedEventArgs> SelectionChanged;
    }

    public class SelectionChangedEventArgs : EventArgs
    {
        public string Path { get; }
        public bool? IsChecked { get; }
        public MonitoringStatus Status { get; }

        public SelectionChangedEventArgs(string path, bool? isChecked, MonitoringStatus status)
        {
            Path = path;
            IsChecked = isChecked;
            Status = status;
        }
    }
}
