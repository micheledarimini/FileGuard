using System;
using System.Collections.Generic;
using System.ComponentModel;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    /// <summary>
    /// Definisce il contratto base per un nodo nell'albero.
    /// </summary>
    public interface ITreeNode : INotifyPropertyChanged
    {
        /// <summary>
        /// Identificatore univoco del nodo.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Nome visualizzato del nodo.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Nodo padre.
        /// </summary>
        ITreeNode? Parent { get; set; }

        /// <summary>
        /// Figli del nodo.
        /// </summary>
        IReadOnlyList<ITreeNode> Children { get; }

        /// <summary>
        /// Indica se il nodo è selezionato.
        /// </summary>
        bool? IsChecked { get; set; }

        /// <summary>
        /// Indica se il nodo è espanso.
        /// </summary>
        bool IsExpanded { get; set; }

        /// <summary>
        /// Indica se il nodo è una directory.
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// Stato di monitoraggio del nodo.
        /// </summary>
        MonitoringStatus MonitoringStatus { get; }

        /// <summary>
        /// Evento scatenato quando cambia lo stato di selezione.
        /// </summary>
        event EventHandler<TreeNodeStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Carica i figli del nodo.
        /// </summary>
        /// <param name="forceReload">Se true, forza il ricaricamento anche se i figli sono già stati caricati</param>
        void LoadChildren(bool forceReload = false);

        /// <summary>
        /// Aggiorna lo stato del nodo senza propagazione.
        /// </summary>
        void UpdateState(bool? isChecked, MonitoringStatus status);
    }

    /// <summary>
    /// Argomenti per l'evento di cambio stato di un nodo.
    /// </summary>
    public class TreeNodeStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Percorso del nodo che ha cambiato stato.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Nuovo stato di selezione.
        /// </summary>
        public bool? IsChecked { get; }

        /// <summary>
        /// Nuovo stato di monitoraggio.
        /// </summary>
        public MonitoringStatus Status { get; }

        /// <summary>
        /// Indica se il cambiamento deve essere propagato ai figli.
        /// </summary>
        public bool PropagateToChildren { get; }

        public TreeNodeStateChangedEventArgs(string path, bool? isChecked, MonitoringStatus status, bool propagateToChildren)
        {
            Path = path;
            IsChecked = isChecked;
            Status = status;
            PropagateToChildren = propagateToChildren;
        }
    }
}
