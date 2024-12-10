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
using FileGuard.Core.Logging;

namespace FileGuard.Core.ViewModels
{
    public abstract class TreeNodeViewModel : ITreeNode
    {
        private class PropagationResult
        {
            public int ProcessedNodes { get; set; }
            public List<string> Errors { get; } = new List<string>();
            public bool HasErrors => Errors.Any();
        }

        protected readonly Dispatcher dispatcher;
        protected readonly ObservableCollection<ITreeNode> children;
        protected readonly ILogger logger;
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
                    // Salva lo stato precedente per eventuale ripristino
                    var previousState = isChecked;
                    var previousStatus = monitoringStatus;

                    try
                    {
                        isUpdatingState = true;
                        logger.LogDebug($"Inizio aggiornamento stato: {previousState} -> {value}", nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] Inizio aggiornamento stato: {previousState} -> {value}");

                        // Determina il nuovo stato
                        var newState = value ?? false;
                        var newStatus = newState ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored;

                        // Aggiorna questo nodo
                        SetStateInternal(newState, newStatus);

                        // Propaga ai figli e gestisce eventuali errori
                        var propagationResult = PropagateToChildren(newState);
                        if (propagationResult.HasErrors)
                        {
                            // Se ci sono errori nella propagazione, imposta stato parziale
                            SetStateInternal(null, MonitoringStatus.PartiallyMonitored);
                            logger.LogDebug($"Impostato stato parziale a causa di errori nella propagazione", nameof(TreeNodeViewModel));
                            Trace.WriteLine($"[TreeNodeViewModel] Impostato stato parziale a causa di errori nella propagazione");
                        }

                        // Aggiorna la catena dei genitori
                        UpdateParentChain();

                        // Notifica il cambiamento di stato
                        OnStateChanged(new TreeNodeStateChangedEventArgs(Path, isChecked, monitoringStatus, true));
                        
                        logger.LogDebug($"Aggiornamento stato completato. Stato finale: {isChecked}, Status: {monitoringStatus}", nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] Aggiornamento stato completato. Stato finale: {isChecked}, Status: {monitoringStatus}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Errore nell'aggiornamento dello stato per {Path}", ex, nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] Errore nell'aggiornamento dello stato per {Path}: {ex.Message}");
                        
                        // Ripristina lo stato precedente in caso di errore
                        try
                        {
                            SetStateInternal(previousState, previousStatus);
                            logger.LogDebug($"Ripristinato stato precedente: {previousState}", nameof(TreeNodeViewModel));
                            Trace.WriteLine($"[TreeNodeViewModel] Ripristinato stato precedente: {previousState}");
                        }
                        catch (Exception restoreEx)
                        {
                            logger.LogError($"Errore nel ripristino dello stato per {Path}", restoreEx, nameof(TreeNodeViewModel));
                            Trace.WriteLine($"[TreeNodeViewModel] Errore nel ripristino dello stato per {Path}: {restoreEx.Message}");
                        }
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
                        logger.LogDebug($"Espansione nodo: {Path}", nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] Espansione nodo: {Path}");
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
            logger = LoggerFactory.GetDefaultLogger();
            
            logger.LogDebug($"Creazione nodo: {path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Creazione nodo: {path}");
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
                    logger.LogDebug($"Aggiornamento stato diretto: {Path} => IsChecked: {isChecked}, Status: {status}", nameof(TreeNodeViewModel));
                    Trace.WriteLine($"[TreeNodeViewModel] Aggiornamento stato diretto: {Path} => IsChecked: {isChecked}, Status: {status}");
                    
                    SetStateInternal(isChecked, status);

                    // Se lo stato è definitivo (true/false), propaga ai figli
                    if (isChecked.HasValue)
                    {
                        PropagateToChildren(isChecked.Value);
                    }

                    // Notifica il cambiamento di stato
                    OnStateChanged(new TreeNodeStateChangedEventArgs(Path, isChecked, status, false));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Errore nell'aggiornamento diretto dello stato per {Path}", ex, nameof(TreeNodeViewModel));
                    Trace.WriteLine($"[TreeNodeViewModel] Errore nell'aggiornamento diretto dello stato per {Path}: {ex.Message}");
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
                
                logger.LogDebug($"Stato interno aggiornato: {Path} => IsChecked: {state}, Status: {status}", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Stato interno aggiornato: {Path} => IsChecked: {state}, Status: {status}");
            }
        }

        private PropagationResult PropagateToChildren(bool state)
        {
            var result = new PropagationResult();
            var totalNodes = Children.Count;

            logger.LogDebug($"Inizio propagazione stato {state} a {totalNodes} figli per {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Inizio propagazione stato {state} a {totalNodes} figli per {Path}");

            foreach (var child in Children)
            {
                if (child is TreeNodeViewModel node)
                {
                    try
                    {
                        logger.LogDebug($"Propagazione stato {state} al figlio {node.Path}", nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] Propagazione stato {state} al figlio {node.Path}");
                        
                        node.UpdateState(state, state ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored);
                        result.ProcessedNodes++;
                    }
                    catch (Exception ex)
                    {
                        var error = $"Errore durante la propagazione a {node.Path}";
                        result.Errors.Add(error);
                        logger.LogError(error, ex, nameof(TreeNodeViewModel));
                        Trace.WriteLine($"[TreeNodeViewModel] {error}: {ex.Message}");
                    }
                }
            }

            // Log riepilogativo
            if (result.HasErrors)
            {
                logger.LogDebug($"Propagazione completata con {result.Errors.Count} errori per {Path}. " +
                              $"Nodi processati: {result.ProcessedNodes}/{totalNodes}", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Propagazione completata con {result.Errors.Count} errori per {Path}. " +
                              $"Nodi processati: {result.ProcessedNodes}/{totalNodes}");
            }
            else
            {
                logger.LogDebug($"Propagazione completata con successo per {Path}. " +
                              $"Nodi processati: {result.ProcessedNodes}/{totalNodes}", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Propagazione completata con successo per {Path}. " +
                              $"Nodi processati: {result.ProcessedNodes}/{totalNodes}");
            }

            return result;
        }

        private void UpdateParentChain()
        {
            logger.LogDebug($"Inizio aggiornamento catena genitori per {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Inizio aggiornamento catena genitori per {Path}");
            
            var current = Parent as TreeNodeViewModel;
            while (current != null)
            {
                current.UpdateFromChildren();
                current = current.Parent as TreeNodeViewModel;
            }
            
            logger.LogDebug($"Completato aggiornamento catena genitori per {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Completato aggiornamento catena genitori per {Path}");
        }

        private void UpdateFromChildren()
        {
            logger.LogDebug($"Aggiornamento stato da figli per {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Aggiornamento stato da figli per {Path}");

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
                    logger.LogDebug($"Impostato stato indeterminato per {Path} (figlio indeterminato)", nameof(TreeNodeViewModel));
                    Trace.WriteLine($"[TreeNodeViewModel] Impostato stato indeterminato per {Path} (figlio indeterminato)");
                    return;
                }
            }

            if (selectedCount == totalCount)
            {
                SetStateInternal(true, MonitoringStatus.FullyMonitored);
                logger.LogDebug($"Impostato stato completamente monitorato per {Path} ({selectedCount}/{totalCount})", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Impostato stato completamente monitorato per {Path} ({selectedCount}/{totalCount})");
            }
            else if (selectedCount == 0)
            {
                SetStateInternal(false, MonitoringStatus.NotMonitored);
                logger.LogDebug($"Impostato stato non monitorato per {Path} (0/{totalCount})", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Impostato stato non monitorato per {Path} (0/{totalCount})");
            }
            else
            {
                SetStateInternal(null, MonitoringStatus.PartiallyMonitored);
                logger.LogDebug($"Impostato stato parzialmente monitorato per {Path} ({selectedCount}/{totalCount})", nameof(TreeNodeViewModel));
                Trace.WriteLine($"[TreeNodeViewModel] Impostato stato parzialmente monitorato per {Path} ({selectedCount}/{totalCount})");
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
            logger.LogDebug($"Aggiunta figlio: {child.Path} a {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Aggiunta figlio: {child.Path} a {Path}");
            
            child.Parent = this;
            children.Add(child);
        }

        protected void RemoveChild(ITreeNode child)
        {
            logger.LogDebug($"Rimozione figlio: {child.Path} da {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Rimozione figlio: {child.Path} da {Path}");
            
            child.Parent = null;
            children.Remove(child);
        }

        protected void ClearChildren()
        {
            logger.LogDebug($"Pulizia figli per {Path}", nameof(TreeNodeViewModel));
            Trace.WriteLine($"[TreeNodeViewModel] Pulizia figli per {Path}");
            
            foreach (var child in children.ToList())
            {
                child.Parent = null;
            }
            children.Clear();
        }
    }
}
