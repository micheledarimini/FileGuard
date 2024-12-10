using System;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface INotificationService
    {
        void ShowFileNotification(string filePath, FileNode fileNode);
        void ShowMessage(string message, string title, NotificationType type);
        event EventHandler<FileNotificationEventArgs> NotificationActionSelected;
    }

    public enum NotificationType
    {
        Information,
        Warning,
        Error,
        Question
    }

    public class FileNotificationEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileNode FileNode { get; }
        public bool IsMonitoringRequested { get; }

        public FileNotificationEventArgs(string filePath, FileNode fileNode, bool isMonitoringRequested)
        {
            FilePath = filePath;
            FileNode = fileNode;
            IsMonitoringRequested = isMonitoringRequested;
        }
    }
}
