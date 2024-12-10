using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using FileGuard.Core.Logging;
using FileGuard.Core.Models;
using FileGuard.Core.Utilities;
using FileGuard.Core.ViewModels;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemEventProcessor
    {
        private readonly ObservableCollection<FileChangeInfo> _changes;
        private readonly ILogger _logger;
        private const int MaxChanges = 1000;

        public FileSystemEventProcessor(ObservableCollection<FileChangeInfo> changes, ILogger? logger = null)
        {
            _changes = changes;
            _logger = logger ?? LoggerFactory.GetDefaultLogger();
        }

        public void ProcessChange(FileSystemEventArgs e, Action<string> nodeUpdateCallback)
        {
            OperationHandler.ExecuteSafe(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Aggiorna il nodo
                    nodeUpdateCallback(e.FullPath);

                    // Crea e aggiungi l'informazione sul cambiamento
                    var changeInfo = CreateChangeInfo(e);
                    AddChange(changeInfo);
                });
            }, _logger, nameof(FileSystemEventProcessor), "ProcessChange");
        }

        private FileChangeInfo CreateChangeInfo(FileSystemEventArgs e)
        {
            return new FileChangeInfo
            {
                Timestamp = DateTime.Now,
                Path = e.FullPath,
                Type = e.ChangeType.ToString(),
                Description = GetChangeDescription(e)
            };
        }

        private string GetChangeDescription(FileSystemEventArgs e)
        {
            return e.ChangeType switch
            {
                WatcherChangeTypes.Created => $"Creato nuovo {(Directory.Exists(e.FullPath) ? "cartella" : "file")}",
                WatcherChangeTypes.Deleted => "Elemento eliminato",
                WatcherChangeTypes.Changed => "Contenuto modificato",
                WatcherChangeTypes.Renamed => e is RenamedEventArgs re ? 
                    $"Rinominato da {Path.GetFileName(re.OldFullPath)}" : 
                    "Elemento rinominato",
                _ => "Modifica non specificata"
            };
        }

        private void AddChange(FileChangeInfo change)
        {
            _changes.Insert(0, change);

            // Mantieni solo gli ultimi MaxChanges eventi
            while (_changes.Count > MaxChanges)
            {
                _changes.RemoveAt(_changes.Count - 1);
            }
        }

        public void UpdateNodes(string path, ObservableCollection<FileSystemNodeViewModel> nodes)
        {
            OperationHandler.ExecuteSafe(() =>
            {
                foreach (var node in nodes)
                {
                    if (path.StartsWith(node.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateNodePreservingState(node, path);
                        break;
                    }
                }
            }, _logger, nameof(FileSystemEventProcessor), "UpdateNodes");
        }

        private void UpdateNodePreservingState(FileSystemNodeViewModel node, string changedPath)
        {
            // Salva lo stato di espansione di tutti i nodi nel percorso
            var expansionStates = new Dictionary<string, bool>();
            SaveExpansionStates(node, changedPath, expansionStates);

            // Aggiorna il contenuto del nodo
            node.LoadChildren(true);

            // Ripristina lo stato di espansione
            RestoreExpansionStates(node, expansionStates);
        }

        private void SaveExpansionStates(FileSystemNodeViewModel node, string changedPath, Dictionary<string, bool> states)
        {
            // Salva lo stato del nodo corrente
            states[node.Path] = node.IsExpanded;

            // Se il nodo è espanso, salva anche gli stati dei figli
            if (node.IsExpanded)
            {
                foreach (var child in node.Children.Cast<FileSystemNodeViewModel>())
                {
                    if (changedPath.StartsWith(child.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        SaveExpansionStates(child, changedPath, states);
                    }
                    else
                    {
                        // Per i nodi non nel percorso del cambiamento, salva solo lo stato corrente
                        states[child.Path] = child.IsExpanded;
                    }
                }
            }
        }

        private void RestoreExpansionStates(FileSystemNodeViewModel node, Dictionary<string, bool> states)
        {
            // Ripristina lo stato del nodo corrente se era salvato
            if (states.TryGetValue(node.Path, out bool wasExpanded))
            {
                node.IsExpanded = wasExpanded;
            }

            // Se il nodo era espanso, ripristina anche gli stati dei figli
            if (node.IsExpanded)
            {
                foreach (var child in node.Children.Cast<FileSystemNodeViewModel>())
                {
                    RestoreExpansionStates(child, states);
                }
            }
        }
    }
}
