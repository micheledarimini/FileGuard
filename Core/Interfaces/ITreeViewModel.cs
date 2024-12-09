using System;
using System.Collections.ObjectModel;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface ITreeViewModel
    {
        ObservableCollection<ITreeNode> MonitoredNodes { get; }
        ITreeNode? SelectedNode { get; set; }
        ObservableCollection<FileChangedEventArgs> FileChanges { get; }
        
        void AddFolder(string path);
        void RemoveFolder(ITreeNode? node);
        void SaveState();
    }
}
