using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.Cache
{
    public class StateManager : IStateManager, IDisposable
    {
        private readonly string settingsPath;
        private MonitoredPaths monitoredPaths;
        private readonly object stateLock = new object();
        private bool hasChanges;
        private bool isDisposed;
        private DateTime lastSave = DateTime.MinValue;
        private static readonly TimeSpan SaveThrottle = TimeSpan.FromSeconds(2);
        private bool isSaving;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public StateManager(StateManagerConfig config)
        {
            settingsPath = config?.SettingsPath ?? throw new ArgumentNullException(nameof(config));
            monitoredPaths = LoadState();
        }

        public NodeState? GetOrCreateState(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                if (state != null)
                {
                    Trace.WriteLine($"[StateManager] GetOrCreateState: {path} => IsChecked: {state.IsChecked}, Status: {state.MonitoringStatus}");
                }
                return state;
            }
        }

        public void AddMonitoredPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                if (state != null)
                {
                    hasChanges = true;
                    Trace.WriteLine($"[StateManager] AddMonitoredPath: {path}");
                }
            }
        }

        public void RemoveMonitoredPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            lock (stateLock)
            {
                monitoredPaths.RemoveState(path);
                hasChanges = true;
            }
        }

        public IEnumerable<string> GetMonitoredPaths()
        {
            lock (stateLock)
            {
                return new List<string>(monitoredPaths.RootStates.Keys);
            }
        }

        public MonitoringStatus GetMonitoringStatus(string path)
        {
            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                return state?.MonitoringStatus ?? MonitoringStatus.NotMonitored;
            }
        }

        public bool? GetIsChecked(string path)
        {
            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                return state?.IsChecked ?? false;
            }
        }

        public bool GetIsExpanded(string path)
        {
            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                return state?.IsExpanded ?? false;
            }
        }

        public void UpdateNodeState(string path, MonitoringStatus status, bool? isChecked, bool isExpanded)
        {
            if (string.IsNullOrEmpty(path)) return;

            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                if (state != null)
                {
                    var oldStatus = state.MonitoringStatus;
                    var oldChecked = state.IsChecked;
                    
                    state.MonitoringStatus = status;
                    state.IsChecked = isChecked;
                    state.IsExpanded = isExpanded;
                    state.IsDirty = true;
                    hasChanges = true;

                    if (oldStatus != status || oldChecked != isChecked)
                    {
                        Trace.WriteLine($"[StateManager] UpdateNodeState: {path} => Status: {oldStatus}->{status}, IsChecked: {oldChecked}->{isChecked}");
                    }
                }
            }
        }

        public void UpdateMonitoringStatus(string path, MonitoringStatus status)
        {
            if (string.IsNullOrEmpty(path)) return;

            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                if (state != null)
                {
                    bool? newChecked = status == MonitoringStatus.FullyMonitored ? true :
                                     status == MonitoringStatus.NotMonitored ? false : null;
                    
                    Trace.WriteLine($"[StateManager] UpdateMonitoringStatus: {path} => Status: {status}, NewChecked: {newChecked}");
                    
                    state.PropagateState(newChecked);
                    hasChanges = true;
                }
            }
        }

        public void UpdateIsExpanded(string path, bool isExpanded)
        {
            if (string.IsNullOrEmpty(path)) return;

            lock (stateLock)
            {
                var state = monitoredPaths.GetOrCreateState(path);
                if (state != null)
                {
                    state.IsExpanded = isExpanded;
                    state.IsDirty = true;
                    hasChanges = true;
                }
            }
        }

        private MonitoredPaths LoadState()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var state = JsonSerializer.Deserialize<MonitoredPaths>(json, JsonOptions);
                    if (state != null)
                    {
                        Trace.WriteLine("[StateManager] LoadState: Stato caricato da file");
                        
                        // Log della gerarchia prima della validazione
                        foreach (var rootState in state.RootStates)
                        {
                            LogStateHierarchy(rootState.Value, 0);
                        }
                        
                        // Assegna lo stato prima di usarlo
                        monitoredPaths = state;
                        
                        // Carica gli stati dei figli
                        foreach (var rootState in state.RootStates)
                        {
                            LoadChildStates(rootState.Value);
                        }
                        
                        state.CleanInvalidPaths();
                        state.ValidateHierarchy();
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[StateManager] Errore caricamento stato: {ex.Message}");
            }

            Trace.WriteLine("[StateManager] LoadState: Creato nuovo stato vuoto");
            return new MonitoredPaths();
        }

        private void LoadChildStates(NodeState state)
        {
            if (state.Path == null) return;

            foreach (var child in state.ChildStates.Values)
            {
                if (child.Path != null)
                {
                    // Assicurati che lo stato sia nel MonitoredPaths
                    monitoredPaths.GetOrCreateState(child.Path);
                    LoadChildStates(child);
                }
            }
        }

        private void LogStateHierarchy(NodeState state, int depth)
        {
            var indent = new string(' ', depth * 2);
            Trace.WriteLine($"{indent}[StateManager] Node: {state.Path} => IsChecked: {state.IsChecked}, Status: {state.MonitoringStatus}, ChildCount: {state.ChildStates.Count}");
            
            foreach (var child in state.ChildStates.Values)
            {
                LogStateHierarchy(child, depth + 1);
            }
        }

        public async Task SaveStateToDiskAsync()
        {
            if (!hasChanges || isDisposed || isSaving) return;

            var now = DateTime.Now;
            if (now - lastSave < SaveThrottle) return;

            try
            {
                isSaving = true;
                string json;
                lock (stateLock)
                {
                    json = JsonSerializer.Serialize(monitoredPaths, JsonOptions);
                    hasChanges = false;
                }

                var tempPath = settingsPath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json);
                File.Move(tempPath, settingsPath, true);
                lastSave = now;
                Trace.WriteLine("[StateManager] SaveStateToDisk: Stato salvato su file");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[StateManager] Errore salvataggio stato: {ex.Message}");
                hasChanges = true;
            }
            finally
            {
                isSaving = false;
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                SaveStateToDiskAsync().Wait();
            }
            finally
            {
                isDisposed = true;
            }
        }
    }
}
