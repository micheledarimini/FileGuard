using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FileGuard.Core.ViewModels;

namespace FileGuard.Core.Modules.EventTable
{
    public partial class EventsGridView : UserControl, INotifyPropertyChanged
    {
        private string _searchText = "";
        private string _selectedEventType = "Tutti";
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private ICollectionView? _view;
        private TreeViewModel? _treeViewModel;
        private ObservableCollection<FileChangeInfo>? _fileChanges;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<FileChangeInfo>? FileChanges
        {
            get => _fileChanges;
            private set
            {
                _fileChanges = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileChanges)));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                    _view?.Refresh();
                }
            }
        }

        public string SelectedEventType
        {
            get => _selectedEventType;
            set
            {
                if (value == null) return;
                if (_selectedEventType != value)
                {
                    _selectedEventType = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEventType)));
                    _view?.Refresh();
                }
            }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FromDate)));
                _view?.Refresh();
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                _toDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToDate)));
                _view?.Refresh();
            }
        }

        public EventsGridView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _treeViewModel = Application.Current.MainWindow?.DataContext as TreeViewModel;
            if (_treeViewModel != null)
            {
                FileChanges = _treeViewModel.FileChanges;
                _view = CollectionViewSource.GetDefaultView(FileChanges);
                if (_view != null)
                {
                    _view.Filter = FilterPredicate;
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_view != null)
            {
                _view.Filter = null;
                _view = null;
            }
            _treeViewModel = null;
            FileChanges = null;
        }

        private bool FilterPredicate(object item)
        {
            if (item is not FileChangeInfo change)
                return false;

            // Filtro tipo
            if (_selectedEventType != "Tutti" && !string.Equals(change.Type, _selectedEventType, StringComparison.OrdinalIgnoreCase))
                return false;

            // Filtro data
            if (_fromDate.HasValue && change.Timestamp.Date < _fromDate.Value.Date)
                return false;

            if (_toDate.HasValue && change.Timestamp.Date > _toDate.Value.Date)
                return false;

            // Filtro ricerca
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchTerms = _searchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var searchableText = $"{change.Path} {change.Description} {change.Type}".ToLower();
                return searchTerms.All(term => searchableText.Contains(term));
            }

            return true;
        }
    }
}
