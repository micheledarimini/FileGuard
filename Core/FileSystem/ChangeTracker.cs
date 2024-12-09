using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;

namespace FileGuard.Core.FileSystem
{
    public class ChangeTracker : IChangeTracker
    {
        private readonly Dispatcher dispatcher;
        private readonly int maxItems;
        private readonly ObservableCollection<Models.FileChangedEventArgs> changes;

        public ObservableCollection<Models.FileChangedEventArgs> Changes => changes;

        public ChangeTracker(Dispatcher dispatcher, int maxItems = 100)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.maxItems = maxItems;
            this.changes = new ObservableCollection<Models.FileChangedEventArgs>();
        }

        public void TrackChange(string path, string type, string description)
        {
            if (dispatcher.CheckAccess())
            {
                AddChange(path, type, description);
            }
            else
            {
                dispatcher.BeginInvoke(new Action(() => AddChange(path, type, description)));
            }
        }

        private void AddChange(string path, string type, string description)
        {
            var change = new Models.FileChangedEventArgs(path, type, description);

            // Aggiungi all'inizio della lista
            changes.Insert(0, change);

            // Mantieni la dimensione massima
            while (changes.Count > maxItems)
            {
                changes.RemoveAt(changes.Count - 1);
            }
        }

        public void Clear()
        {
            if (dispatcher.CheckAccess())
            {
                changes.Clear();
            }
            else
            {
                dispatcher.BeginInvoke(new Action(() => changes.Clear()));
            }
        }
    }
}
