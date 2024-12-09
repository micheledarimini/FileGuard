using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FileGuard.Core.Models;

namespace FileGuard.Core.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private readonly Statistics _statistics;

        public int MonitoredFolders
        {
            get => _statistics.MonitoredFolders;
            set
            {
                if (_statistics.MonitoredFolders != value)
                {
                    _statistics.MonitoredFolders = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MonitoredFiles
        {
            get => _statistics.MonitoredFiles;
            set
            {
                if (_statistics.MonitoredFiles != value)
                {
                    _statistics.MonitoredFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BackupSpace
        {
            get => _statistics.FormatBackupSpace();
            set
            {
                // Parsing semplificato per esempio
                if (long.TryParse(value?.Split(' ')[0], out long bytes))
                {
                    _statistics.BackupSpaceBytes = bytes;
                    OnPropertyChanged();
                }
            }
        }

        public int TodayEvents
        {
            get => _statistics.TodayEvents;
            set
            {
                if (_statistics.TodayEvents != value)
                {
                    _statistics.TodayEvents = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastUpdate
        {
            get => _statistics.LastUpdate.ToString("dd/MM/yyyy HH:mm");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _statistics.LastUpdate = date;
                    OnPropertyChanged();
                }
            }
        }

        public StatisticsViewModel()
        {
            _statistics = new Statistics
            {
                MonitoredFolders = 12,
                MonitoredFiles = 48,
                BackupSpaceBytes = 1932735283, // ~1.8 GB
                TodayEvents = 156,
                LastUpdate = DateTime.Now
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateStatistics()
        {
            // TODO: Implementare la logica per aggiornare le statistiche
            // Questo metodo verrà chiamato per aggiornare i dati in tempo reale
            _statistics.LastUpdate = DateTime.Now;
            OnPropertyChanged(nameof(LastUpdate));
        }
    }
}
