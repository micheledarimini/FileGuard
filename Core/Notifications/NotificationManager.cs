using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.UI.Components;

namespace FileGuard.Core.Notifications
{
    public class NotificationManager : INotificationService
    {
        private readonly Dispatcher dispatcher;
        private readonly ConcurrentQueue<Action> notificationQueue;
        private readonly object syncLock = new object();
        private bool isProcessing;

        public event EventHandler<FileNotificationEventArgs>? NotificationActionSelected;

        public NotificationManager()
        {
            this.dispatcher = Application.Current.Dispatcher;
            this.notificationQueue = new ConcurrentQueue<Action>();
        }

        public void ShowFileNotification(string filePath, FileNode fileNode)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            EnqueueNotification(() =>
            {
                NewFilePopup.Show(filePath, fileNode, node =>
                {
                    NotificationActionSelected?.Invoke(this, 
                        new FileNotificationEventArgs(filePath, node, true));
                });
            });
        }

        public void ShowMessage(string message, string title, NotificationType type)
        {
            if (string.IsNullOrEmpty(message)) return;

            EnqueueNotification(() =>
            {
                var icon = type switch
                {
                    NotificationType.Warning => MessageBoxImage.Warning,
                    NotificationType.Error => MessageBoxImage.Error,
                    NotificationType.Question => MessageBoxImage.Question,
                    _ => MessageBoxImage.Information
                };

                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            });
        }

        private void EnqueueNotification(Action notification)
        {
            notificationQueue.Enqueue(notification);
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            lock (syncLock)
            {
                if (isProcessing) return;
                isProcessing = true;
            }

            dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    while (notificationQueue.TryDequeue(out var notification))
                    {
                        notification.Invoke();
                        await System.Threading.Tasks.Task.Delay(500); // Delay tra notifiche
                    }
                }
                finally
                {
                    lock (syncLock)
                    {
                        isProcessing = false;
                    }
                }
            }));
        }
    }
}
