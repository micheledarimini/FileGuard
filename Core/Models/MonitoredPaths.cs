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

            if (!RootStates.TryGetValue(rootPath, out var rootState))
            {
                rootState = new NodeState { Path = rootPath };
                RootStates[rootPath] = rootState;
            }

            if (path.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return rootState;
            }

            var currentState = rootState;
            var pathParts = path.Substring(rootPath.Length).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            var currentPath = rootPath;

            foreach (var part in pathParts)
            {
                currentPath = Path.Combine(currentPath, part);
                
                if (!currentState.ChildStates.TryGetValue(currentPath, out var childState))
                {
                    childState = new NodeState 
                    { 
                        Path = currentPath,
                        ParentPath = currentState.Path
                    };
                    currentState.ChildStates[currentPath] = childState;
                }

                currentState = childState;
            }

            return currentState;
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
            foreach (var state in RootStates.Values)
            {
                state.ValidateHierarchy();
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
            }

            foreach (var child in state.ChildStates.Values)
            {
                CleanInvalidChildren(child);
            }
        }
    }
}
