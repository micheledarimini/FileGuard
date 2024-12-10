using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemEventHandler : IFileSystemEventHandler
    {
        private readonly Dispatcher dispatcher;
        private readonly ConcurrentDictionary<string, DateTime> lastEvents;
        private static readonly TimeSpan EventThreshold = TimeSpan.FromMilliseconds(250);

        public event EventHandler<FileSystemEventArgs>? FileSystemChanged;

        public FileSystemEventHandler(Dispatcher? dispatcher = null)
        {
            this.dispatcher = dispatcher ?? Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            this.lastEvents = new ConcurrentDictionary<string, DateTime>();
        }

        public void HandleFileSystemEvent(object? sender, FileSystemEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (!ShouldProcessEvent(e.FullPath, e.ChangeType.ToString())) return;

            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    NotifyFileSystemChanged(e);
                }
                catch (Exception ex)
                {
                    LogError($"Errore nella gestione dell'evento filesystem: {ex.Message}");
                }
            }));
        }

        private bool ShouldProcessEvent(string path, string eventType)
        {
            var key = $"{path}|{eventType}";
            var now = DateTime.Now;

            if (lastEvents.TryGetValue(key, out var lastTime))
            {
                if (now - lastTime < EventThreshold)
                {
                    return false;
                }
            }

            lastEvents.AddOrUpdate(key, now, (_, _) => now);
            return true;
        }

        private void NotifyFileSystemChanged(FileSystemEventArgs e)
        {
            FileSystemChanged?.Invoke(this, e);
        }

        private void LogError(string message)
        {
            try
            {
                Debug.WriteLine($"FileSystemEventHandler Error: {message}");
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fileguard_error.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n"
                );
            }
            catch { }
        }
    }
}
