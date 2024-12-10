using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;
using System.Threading;
using System.Linq;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemMonitor : IFileSystemMonitor
    {
        private readonly ConcurrentDictionary<string, FileSystemWatcher> watchers;
        private readonly ConcurrentDictionary<string, DateTime> lastEvents;
        private readonly ConcurrentDictionary<string, DateTime> recentFiles;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> pathLocks;
        private static readonly TimeSpan EventThreshold = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan FileStabilityDelay = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan WatcherRestartDelay = TimeSpan.FromSeconds(1);
        private const int WatcherBufferSize = 65536;
        private const int MaxRetries = 3;
        private bool isDisposed;

        public event EventHandler<FileSystemEventArgs>? FileSystemChanged;

        public FileSystemMonitor()
        {
            watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
            lastEvents = new ConcurrentDictionary<string, DateTime>();
            recentFiles = new ConcurrentDictionary<string, DateTime>();
            pathLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public bool StartMonitoring(string path)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(FileSystemMonitor));
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;

            try
            {
                var watcher = watchers.GetOrAdd(path, CreateWatcher);
                return watcher != null && watcher.EnableRaisingEvents;
            }
            catch (Exception ex)
            {
                LogError($"Errore in StartMonitoring: {ex.Message}");
                return false;
            }
        }

        public void StopMonitoring(string path)
        {
            if (isDisposed) return;

            if (watchers.TryRemove(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            if (pathLocks.TryRemove(path, out var semaphore))
            {
                semaphore.Dispose();
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | 
                             NotifyFilters.DirectoryName |
                             NotifyFilters.LastWrite |
                             NotifyFilters.Size |
                             NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                InternalBufferSize = WatcherBufferSize
            };

            watcher.Created += OnFileSystemEvent;
            watcher.Deleted += OnFileSystemEvent;
            watcher.Changed += OnFileSystemEvent;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnWatcherError;

            return watcher;
        }

        private async void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            if (isDisposed) return;

            var semaphore = pathLocks.GetOrAdd(e.FullPath, _ => new SemaphoreSlim(1, 1));
            
            try
            {
                await semaphore.WaitAsync();
                
                if (!ShouldProcessEvent(e.FullPath, e.ChangeType.ToString())) return;

                if (Directory.Exists(e.FullPath))
                {
                    NotifyFileSystemChanged(e);
                    return;
                }

                await Task.Delay(FileStabilityDelay);

                if (e.ChangeType == WatcherChangeTypes.Created && !IsRecentFile(e.FullPath))
                {
                    if (File.Exists(e.FullPath))
                    {
                        NotifyFileSystemChanged(e);
                    }
                }
                else if (File.Exists(e.FullPath) || e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    NotifyFileSystemChanged(e);
                }
            }
            catch (Exception ex)
            {
                LogError($"Errore in OnFileSystemEvent: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (isDisposed) return;

            var semaphore = pathLocks.GetOrAdd(e.FullPath, _ => new SemaphoreSlim(1, 1));
            
            try
            {
                await semaphore.WaitAsync();
                
                if (!ShouldProcessEvent(e.FullPath, "Renamed")) return;

                await Task.Delay(FileStabilityDelay);
                NotifyFileSystemChanged(e);
            }
            catch (Exception ex)
            {
                LogError($"Errore in OnRenamed: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool ShouldProcessEvent(string path, string eventType)
        {
            if (isDisposed) return false;

            var key = $"{path}|{eventType}";
            var now = DateTime.Now;

            return lastEvents.AddOrUpdate(
                key,
                now,
                (_, lastTime) => now - lastTime >= EventThreshold ? now : lastTime
            ) == now;
        }

        private bool IsRecentFile(string path)
        {
            if (isDisposed) return false;

            var now = DateTime.Now;
            if (recentFiles.TryGetValue(path, out var lastTime))
            {
                if (now - lastTime < EventThreshold)
                {
                    return true;
                }
                recentFiles.TryRemove(path, out _);
            }
            
            recentFiles.TryAdd(path, now);
            return false;
        }

        private async void OnWatcherError(object sender, ErrorEventArgs e)
        {
            if (isDisposed || sender is not FileSystemWatcher watcher) return;

            LogError($"Errore FileSystemWatcher: {e.GetException().Message}");
            
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    await Task.Delay(WatcherRestartDelay);
                    
                    if (e.GetException() is InternalBufferOverflowException)
                    {
                        var path = watcher.Path;
                        watcher.Dispose();
                        
                        if (watchers.TryRemove(path, out _))
                        {
                            var newWatcher = CreateWatcher(path);
                            if (watchers.TryAdd(path, newWatcher))
                            {
                                LogError($"Watcher ricreato per {path} dopo buffer overflow");
                                return;
                            }
                        }
                    }
                    else
                    {
                        watcher.EnableRaisingEvents = true;
                        LogError($"Watcher riavviato con successo dopo {retry + 1} tentativi");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Tentativo {retry + 1} di riavvio watcher fallito: {ex.Message}");
                    await Task.Delay(WatcherRestartDelay);
                }
            }

            LogError($"Impossibile riavviare il watcher dopo {MaxRetries} tentativi");
        }

        private void NotifyFileSystemChanged(FileSystemEventArgs e)
        {
            if (!isDisposed)
            {
                FileSystemChanged?.Invoke(this, e);
            }
        }

        private void LogError(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fileguard_error.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n"
                );
            }
            catch { }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    foreach (var watcher in watchers.Values)
                    {
                        try
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.Dispose();
                        }
                        catch (Exception ex)
                        {
                            LogError($"Errore durante la dispose del watcher: {ex.Message}");
                        }
                    }
                    watchers.Clear();

                    foreach (var semaphore in pathLocks.Values)
                    {
                        try
                        {
                            semaphore.Dispose();
                        }
                        catch (Exception ex)
                        {
                            LogError($"Errore durante la dispose del semaforo: {ex.Message}");
                        }
                    }
                    pathLocks.Clear();

                    lastEvents.Clear();
                    recentFiles.Clear();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FileSystemMonitor()
        {
            Dispose(false);
        }
    }
}
