using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Diagnostics;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;

namespace FileGuard.Core.ViewModels
{
    public abstract class TreeNodeViewModel : ITreeNode
    {
        protected readonly Dispatcher dispatcher;
        protected readonly ObservableCollection<ITreeNode> children;
        private ITreeNode? parent;
        private bool? isChecked;
        private bool isExpanded;
        private bool isUpdatingState;
        private MonitoringStatus monitoringStatus;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<TreeNodeStateChangedEventArgs>? StateChanged;

        public string Path { get; }
        public abstract string DisplayName { get; }
        public abstract bool IsDirectory { get; }

        public ITreeNode? Parent
        {
            get => parent;
            set
            {
                if (parent != value)
                {
                    parent = value;
                    OnPropertyChanged();
                }
            }
        }

        public IReadOnlyList<ITreeNode> Children => children;

        public bool? IsChecked
        {
            get => isChecked;
            set
            {
                if (!isUpdatingState)
                {
                    try
                    {
                        isUpdatingState = true;

                        // Determina il nuovo stato
                        var newState = value ?? false;
                        var newStatus = newState ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored;

                        // Aggiorna questo nodo
                        SetStateInternal(newState, newStatus);

                        // Propaga ai figli
                        PropagateToChildren(newState);

                        // Aggiorna la catena dei genitori
                        UpdateParentChain();
                    }
                    finally
                    {
                        isUpdatingState = false;
                    }
                }
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged();

                    if (value)
                    {
                        LoadChildren();
                    }
                }
            }
        }

        public MonitoringStatus MonitoringStatus
        {
            get => monitoringStatus;
            private set
            {
                if (monitoringStatus != value)
                {
                    monitoringStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        protected TreeNodeViewModel(string path, Dispatcher? dispatcher = null)
        {
            Path = path;
            this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            children = new ObservableCollection<ITreeNode>();
            isChecked = false;
            monitoringStatus = MonitoringStatus.NotMonitored;
        }

        public virtual void LoadChildren(bool forceReload = false)
        {
            // Implementato nelle classi derivate
        }

        public void UpdateState(bool? isChecked, MonitoringStatus status)
        {
            if (!isUpdatingState)
            {
                try
                {
                    isUpdatingState = true;
                    SetStateInternal(isChecked, status);

                    // Se lo stato è definitivo (true/false), propaga ai figli
                    if (isChecked.HasValue)
                    {
                        PropagateToChildren(isChecked.Value);
                    }
                }
                finally
                {
                    isUpdatingState = false;
                }
            }
        }

        private void SetStateInternal(bool? state, MonitoringStatus status)
        {
            if (this.isChecked != state || MonitoringStatus != status)
            {
                this.isChecked = state;
                MonitoringStatus = status;
                OnPropertyChanged(nameof(IsChecked));
                OnStateChanged(new TreeNodeStateChangedEventArgs(Path, state, status, false));
            }
        }

        private void PropagateToChildren(bool state)
        {
            foreach (var child in Children)
            {
                if (child is TreeNodeViewModel node)
                {
                    node.UpdateState(state, state ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);
                }
            }

            OnStateChanged(new TreeNodeStateChangedEventArgs(Path, state, MonitoringStatus, true));
        }

        private void UpdateParentChain()
        {
            var current = Parent as TreeNodeViewModel;
            while (current != null)
            {
                current.UpdateFromChildren();
                current = current.Parent as TreeNodeViewModel;
            }
        }

        private void UpdateFromChildren()
        {
            if (Children.Count == 0)
            {
                SetStateInternal(false, MonitoringStatus.NotMonitored);
                return;
            }

            int selectedCount = 0;
            int totalCount = Children.Count;

            foreach (var child in Children)
            {
                if (child.IsChecked == true)
                    selectedCount++;
                else if (child.IsChecked == null)
                {
                    // Se un figlio è indeterminato, il padre è indeterminato
                    SetStateInternal(null, MonitoringStatus.PartiallyMonitored);
                    return;
                }
            }

            if (selectedCount == totalCount)
            {
                SetStateInternal(true, MonitoringStatus.FullyMonitored);
            }
            else if (selectedCount == 0)
            {
                SetStateInternal(false, MonitoringStatus.NotMonitored);
            }
            else
            {
                SetStateInternal(null, MonitoringStatus.PartiallyMonitored);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        protected virtual void OnStateChanged(TreeNodeStateChangedEventArgs e)
        {
            if (dispatcher.CheckAccess())
            {
                StateChanged?.Invoke(this, e);
            }
            else
            {
                dispatcher.Invoke(() => StateChanged?.Invoke(this, e));
            }
        }

        protected void AddChild(ITreeNode child)
        {
            child.Parent = this;
            children.Add(child);
        }

        protected void RemoveChild(ITreeNode child)
        {
            child.Parent = null;
            children.Remove(child);
        }

        protected void ClearChildren()
        {
            foreach (var child in children.ToList())
            {
                child.Parent = null;
            }
            children.Clear();
        }
    }
}
