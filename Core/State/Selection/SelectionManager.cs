using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.ViewModels;

namespace FileGuard.Core.State.Selection
{
    public class SelectionManager : ISelectionManager
    {
        private readonly IStateManager stateManager;
        private readonly ConcurrentDictionary<string, NodeState> selectionCache;
        private readonly HashSet<string> pendingNotifications;
        private bool isBatchOperation;

        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

        public SelectionManager(IStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.selectionCache = new ConcurrentDictionary<string, NodeState>(StringComparer.OrdinalIgnoreCase);
            this.pendingNotifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetNodeSelection(string path, bool? state)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                isBatchOperation = true;

                // Aggiorna lo stato nel cache
                var nodeState = GetOrCreateNodeState(path);
                if (nodeState.IsChecked != state)
                {
                    nodeState.IsChecked = state;
                    nodeState.MonitoringStatus = state == true ? MonitoringStatus.FullyMonitored :
                                               state == false ? MonitoringStatus.NotMonitored :
                                               MonitoringStatus.PartiallyMonitored;

                    // Persisti il cambiamento
                    stateManager.UpdateNodeState(path, nodeState.MonitoringStatus, state, nodeState.IsExpanded);

                    // Notifica il cambiamento
                    QueueNotification(path, state, nodeState.MonitoringStatus);
                }
            }
            finally
            {
                isBatchOperation = false;
                NotifyPendingChanges();
            }
        }

        public bool? GetNodeSelection(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return GetOrCreateNodeState(path).IsChecked;
        }

        public MonitoringStatus GetMonitoringStatus(string path)
        {
            if (string.IsNullOrEmpty(path)) return MonitoringStatus.NotMonitored;
            return GetOrCreateNodeState(path).MonitoringStatus;
        }

        public void LoadNodeState(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            var state = stateManager.GetOrCreateState(path);
            if (state != null)
            {
                selectionCache.AddOrUpdate(path, state, (_, __) => state);
            }
        }

        private NodeState GetOrCreateNodeState(string path)
        {
            return selectionCache.GetOrAdd(path, p =>
            {
                var state = stateManager.GetOrCreateState(p);
                return state ?? new NodeState { Path = p };
            });
        }

        private void QueueNotification(string path, bool? state, MonitoringStatus status)
        {
            if (!isBatchOperation)
            {
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(path, state, status));
            }
            else
            {
                pendingNotifications.Add(path);
            }
        }

        private void NotifyPendingChanges()
        {
            foreach (var path in pendingNotifications)
            {
                var state = GetOrCreateNodeState(path);
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(path, state.IsChecked, state.MonitoringStatus));
            }
            pendingNotifications.Clear();
        }
    }
}
