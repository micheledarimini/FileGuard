using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.Logging;
using FileGuard.Core.State;
using FileGuard.Core.Utilities;

namespace FileGuard.Core.ViewModels
{
    public abstract class TreeNodeViewModel : ITreeNode
    {
        protected readonly Dispatcher dispatcher;
        protected readonly ObservableCollection<ITreeNode> children;
        protected readonly ILogger logger;
        protected readonly NodeStateHandler stateHandler;
        private ITreeNode? parent;
        private bool isExpanded;
        private bool isUpdatingExpansion;

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
            get => stateHandler.IsChecked;
            set
            {
                OperationHandler.ExecuteSafe(() =>
                {
                    if (value.HasValue)
                    {
                        var newStatus = value.Value ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored;
                        stateHandler.UpdateState(value, newStatus);

                        // Forza la propagazione anche se la cartella è chiusa
                        PropagateStateToAllChildren(value.Value);
                    }
                    else
                    {
                        stateHandler.UpdateState(null, MonitoringStatus.PartiallyMonitored);
                    }
                }, logger, nameof(TreeNodeViewModel), "SetIsChecked");
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value && !isUpdatingExpansion)
                {
                    try
                    {
                        isUpdatingExpansion = true;
                        isExpanded = value;
                        OnPropertyChanged();

                        if (value)
                        {
                            OperationHandler.ExecuteSafe(() =>
                            {
                                LoadChildren();
                                // Dopo il caricamento, assicura che i figli abbiano lo stato corretto
                                if (IsChecked.HasValue)
                                {
                                    PropagateStateToAllChildren(IsChecked.Value);
                                }
                            }, logger, nameof(TreeNodeViewModel), "LoadChildren");
                        }
                    }
                    finally
                    {
                        isUpdatingExpansion = false;
                    }
                }
            }
        }

        public MonitoringStatus MonitoringStatus => stateHandler.Status;

        protected TreeNodeViewModel(string path, Dispatcher? dispatcher = null)
        {
            Path = path;
            this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            children = new ObservableCollection<ITreeNode>();
            logger = LoggerFactory.GetDefaultLogger();
            stateHandler = new NodeStateHandler(path, logger);
            isUpdatingExpansion = false;

            stateHandler.StateChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(MonitoringStatus));

                if (e.ShouldPropagate)
                {
                    PropagateStateToAllChildren(e.NewState ?? false);
                }

                UpdateParentChain();
                OnStateChanged(new TreeNodeStateChangedEventArgs(Path, e.NewState, e.NewStatus, e.ShouldPropagate));
            };
        }

        public virtual void LoadChildren(bool forceReload = false)
        {
            // Implementato nelle classi derivate
        }

        public void UpdateState(bool? isChecked, MonitoringStatus status)
        {
            OperationHandler.ExecuteSafe(() =>
            {
                stateHandler.UpdateState(isChecked, status, true);

                // Se lo stato è definitivo, propaga ai figli indipendentemente dallo stato di espansione
                if (isChecked.HasValue)
                {
                    PropagateStateToAllChildren(isChecked.Value);
                }
            }, logger, nameof(TreeNodeViewModel), "UpdateState");
        }

        private void PropagateStateToAllChildren(bool state)
        {
            foreach (var child in Children.OfType<TreeNodeViewModel>())
            {
                child.UpdateState(state, state ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);

                // Propaga ricorsivamente anche se la cartella è chiusa
                if (child.IsDirectory)
                {
                    child.PropagateStateToAllChildren(state);
                }
            }
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
            OperationHandler.ExecuteSafe(() =>
            {
                stateHandler.UpdateFromChildren(Children);
            }, logger, nameof(TreeNodeViewModel), "UpdateFromChildren");
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
            OperationHandler.ExecuteSafe(() =>
            {
                child.Parent = this;
                children.Add(child);

                // Assicura che il nuovo figlio abbia lo stato corretto
                if (child is TreeNodeViewModel nodeChild && IsChecked.HasValue)
                {
                    nodeChild.UpdateState(IsChecked.Value,
                        IsChecked.Value ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);
                }
            }, logger, nameof(TreeNodeViewModel), "AddChild");
        }

        protected void RemoveChild(ITreeNode child)
        {
            OperationHandler.ExecuteSafe(() =>
            {
                child.Parent = null;
                children.Remove(child);
            }, logger, nameof(TreeNodeViewModel), "RemoveChild");
        }

        protected void ClearChildren()
        {
            OperationHandler.ExecuteSafe(() =>
            {
                foreach (var child in children.ToList())
                {
                    child.Parent = null;
                }
                children.Clear();
            }, logger, nameof(TreeNodeViewModel), "ClearChildren");
        }
    }
}
