using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace FileGuard.Core.Models
{
    public class MonitoredPaths
    {
        private Dictionary<string, NodeState>? rootStates;

        [JsonPropertyName("rootStates")]
        public Dictionary<string, NodeState> RootStates 
        { 
            get 
            {
                rootStates ??= new Dictionary<string, NodeState>(StringComparer.OrdinalIgnoreCase);
                return rootStates;
            }
            set
            {
                rootStates = value ?? new Dictionary<string, NodeState>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public MonitoredPaths() { }

        public NodeState? GetOrCreateState(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var rootPath = GetRootPath(path);
            
            if (rootPath == null && Directory.Exists(path))
            {
                rootPath = path;
            }

            if (rootPath == null) return null;

            // Ottieni o crea il root state
            if (!RootStates.TryGetValue(rootPath, out var rootState))
            {
                rootState = new NodeState { Path = rootPath };
                RootStates[rootPath] = rootState;
                Trace.WriteLine($"[MonitoredPaths] Creato nuovo root state: {rootPath}");
            }

            if (path.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return rootState;
            }

            // Cerca lo stato esistente nella gerarchia
            NodeState? existingState = FindExistingState(rootState, path);
            if (existingState != null)
            {
                Trace.WriteLine($"[MonitoredPaths] Trovato state esistente: {path} => IsChecked: {existingState.IsChecked}, Status: {existingState.MonitoringStatus}");
                return existingState;
            }

            // Se non esiste, crea la gerarchia di stati
            var currentState = rootState;
            var pathParts = path.Substring(rootPath.Length).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            var currentPath = rootPath;

            foreach (var part in pathParts)
            {
                currentPath = Path.Combine(currentPath, part);

                // Cerca prima lo stato esistente
                if (!currentState.ChildStates.TryGetValue(currentPath, out var childState))
                {
                    // Se non esiste, crea un nuovo stato con stato NotMonitored
                    childState = new NodeState 
                    { 
                        Path = currentPath,
                        ParentPath = currentState.Path,
                        IsChecked = false,
                        MonitoringStatus = MonitoringStatus.NotMonitored
                    };
                    currentState.ChildStates[currentPath] = childState;
                    Trace.WriteLine($"[MonitoredPaths] Creato nuovo child state: {currentPath} (Parent: {currentState.Path}) => IsChecked: {childState.IsChecked}, Status: {childState.MonitoringStatus}");
                }

                currentState = childState;
            }

            return currentState;
        }

        private NodeState? FindExistingState(NodeState root, string path)
        {
            if (root.Path?.Equals(path, StringComparison.OrdinalIgnoreCase) == true)
            {
                return root;
            }

            foreach (var child in root.ChildStates.Values)
            {
                if (child.Path?.Equals(path, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return child;
                }

                var found = FindExistingState(child, path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private string? GetRootPath(string path)
        {
            return RootStates.Keys
                .Where(root => path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(root => root.Length)
                .FirstOrDefault();
        }

        public void RemoveState(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var rootPath = GetRootPath(path);
            if (rootPath == null) return;

            if (path.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                RootStates.Remove(path);
                Trace.WriteLine($"[MonitoredPaths] Rimosso root state: {path}");
                return;
            }

            if (RootStates.TryGetValue(rootPath, out var rootState))
            {
                RemoveChildState(rootState, path);
            }
        }

        private void RemoveChildState(NodeState parent, string path)
        {
            foreach (var child in parent.ChildStates.Values.ToList())
            {
                if (child.Path != null)
                {
                    if (child.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                    {
                        parent.ChildStates.Remove(path);
                        Trace.WriteLine($"[MonitoredPaths] Rimosso child state: {path} (Parent: {parent.Path})");
                        return;
                    }

                    if (path.StartsWith(child.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        RemoveChildState(child, path);
                    }
                }
            }
        }

        public void ValidateHierarchy()
        {
            Trace.WriteLine("[MonitoredPaths] Validazione gerarchia");
            foreach (var state in RootStates.Values)
            {
                state.ValidateHierarchy();
                Trace.WriteLine($"[MonitoredPaths] Root validato: {state.Path} => IsChecked: {state.IsChecked}, Status: {state.MonitoringStatus}, ChildCount: {state.ChildStates.Count}");
            }
        }

        public void CleanInvalidPaths()
        {
            var invalidRoots = RootStates.Keys
                .Where(path => !Directory.Exists(path))
                .ToList();

            foreach (var path in invalidRoots)
            {
                RootStates.Remove(path);
                Trace.WriteLine($"[MonitoredPaths] Rimosso root non valido: {path}");
            }

            foreach (var root in RootStates.Values)
            {
                CleanInvalidChildren(root);
            }
        }

        private void CleanInvalidChildren(NodeState state)
        {
            if (state.Path == null) return;

            var invalidChildren = state.ChildStates
                .Where(kvp => !File.Exists(kvp.Key) && !Directory.Exists(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var path in invalidChildren)
            {
                state.ChildStates.Remove(path);
                Trace.WriteLine($"[MonitoredPaths] Rimosso child non valido: {path} (Parent: {state.Path})");
            }

            foreach (var child in state.ChildStates.Values)
            {
                CleanInvalidChildren(child);
            }
        }
    }
}
