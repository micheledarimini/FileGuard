using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FileGuard.Core.Storage;
using FileGuard.Core.Logging;

namespace FileGuard.Core.UI.Components.Dialogs
{
    public partial class EventArchiveSettingsDialog : Window, INotifyPropertyChanged
    {
        private readonly EventArchive _archive;
        private decimal _storageSize = 10;
        private string _storageUnit = "MB";
        private string _storageStats = "";
        private readonly string _settingsPath;
        private System.Windows.Threading.DispatcherTimer _statsTimer;
        private readonly ILogger _logger;
        private long _lastKnownSize;
        private readonly CultureInfo _cultureInfo;
        private string _rawInput = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string StorageSize
        {
            get => _rawInput;
            set
            {
                _rawInput = value;
                _logger.LogDebug("Input grezzo: {0}", value);

                if (string.IsNullOrWhiteSpace(value))
                {
                    UpdateInputValidation(true);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageSize)));
                    return;
                }

                // Normalizza il separatore decimale
                string normalizedValue = value.Replace('.', _cultureInfo.NumberFormat.NumberDecimalSeparator[0])
                                            .Replace(',', _cultureInfo.NumberFormat.NumberDecimalSeparator[0]);
                
                if (decimal.TryParse(normalizedValue, NumberStyles.AllowDecimalPoint, _cultureInfo, out decimal newSize))
                {
                    if (newSize >= 0)
                    {
                        _storageSize = newSize;
                        _logger.LogDebug("Valore parsato: {0}", newSize);
                        UpdateInputValidation(true);
                    }
                    else
                    {
                        _logger.LogWarning("Valore negativo non valido: {0}", newSize);
                        UpdateInputValidation(false);
                    }
                }
                else
                {
                    _logger.LogWarning("Parsing fallito per: {0}", normalizedValue);
                    UpdateInputValidation(false);
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageSize)));
            }
        }

        public string StorageUnit
        {
            get => _storageUnit;
            set
            {
                if (_storageUnit != value)
                {
                    _storageUnit = value;
                    _logger.LogDebug("Unità di misura cambiata a: {0}", value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageUnit)));
                }
            }
        }

        public string StorageStats
        {
            get => _storageStats;
            private set
            {
                if (_storageStats != value)
                {
                    _storageStats = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageStats)));
                }
            }
        }

        public EventArchiveSettingsDialog(EventArchive archive)
        {
            InitializeComponent();
            DataContext = this;
            _archive = archive;
            _logger = LoggerFactory.CreateLogger("EventArchiveSettingsDialog");
            _cultureInfo = CultureInfo.CurrentCulture;

            var settingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Build", "Release");
            Directory.CreateDirectory(settingsDir);
            _settingsPath = Path.Combine(settingsDir, "settings.json");

            LoadSettings();
            UpdateStorageStats();

            _statsTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statsTimer.Tick += (s, e) => UpdateStorageStats();
            _statsTimer.Start();

            _logger.LogDebug("Dialog inizializzato");
        }

        private void UpdateInputValidation(bool isValid)
        {
            var textBox = StorageSizeTextBox;
            if (textBox == null) return;

            if (!isValid && !string.IsNullOrWhiteSpace(_rawInput))
            {
                textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                textBox.ToolTip = "Inserire un valore numerico valido maggiore o uguale a 0";
            }
            else
            {
                textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                textBox.ToolTip = null;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _logger.LogDebug("Caricamento impostazioni da: {0}", _settingsPath);

                    var settings = System.Text.Json.JsonSerializer.Deserialize<ArchiveSettings>(json);
                    if (settings != null)
                    {
                        _storageSize = settings.MaxStorageSize;
                        _storageUnit = settings.StorageUnit;
                        _rawInput = _storageSize.ToString("0.##", _cultureInfo);
                        _logger.LogDebug("Impostazioni caricate - Size: {0}, Unit: {1}", _storageSize, _storageUnit);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageSize)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageUnit)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento impostazioni", ex);
                MessageBox.Show($"Errore durante il caricamento delle impostazioni: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new ArchiveSettings
                {
                    MaxStorageSize = _storageSize,
                    StorageUnit = _storageUnit
                };

                _logger.LogDebug("Salvataggio impostazioni - Size: {0}, Unit: {1}", _storageSize, _storageUnit);

                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsPath, json);
                _logger.LogDebug("Impostazioni salvate con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio impostazioni", ex);
                MessageBox.Show($"Errore durante il salvataggio delle impostazioni: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStorageStats()
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "EventArchive.db");
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    fileInfo.Refresh();
                    
                    var currentSize = fileInfo.Length;
                    if (currentSize != _lastKnownSize)
                    {
                        _lastKnownSize = currentSize;
                        _logger.LogDebug("Dimensione database aggiornata: {0} bytes", currentSize);
                        
                        var sizeInMB = Math.Round(currentSize / (1024.0 * 1024.0), 2);
                        StorageStats = $"Dimensione attuale: {sizeInMB.ToString("0.##", _cultureInfo)} MB";
                        _logger.LogDebug("Statistiche aggiornate: {0}", StorageStats);
                    }
                }
                else
                {
                    if (_lastKnownSize != 0)
                    {
                        _lastKnownSize = 0;
                        _logger.LogWarning("Database non trovato in: {0}", dbPath);
                        StorageStats = "Database non ancora creato";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento statistiche", ex);
                StorageStats = "Errore nel calcolo delle statistiche";
            }
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Sei sicuro di voler eliminare tutti gli eventi archiviati?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _logger.LogDebug("Avvio eliminazione eventi");
                    _archive.DeleteAllEvents();
                    _logger.LogDebug("Eventi eliminati con successo");
                    UpdateStorageStats();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Errore eliminazione eventi", ex);
                    MessageBox.Show($"Errore durante l'eliminazione degli eventi: {ex.Message}",
                        "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_rawInput))
            {
                _logger.LogWarning("Campo vuoto non valido");
                MessageBox.Show("Inserire un valore valido per lo spazio massimo.", 
                    "Campo vuoto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_storageSize <= 0)
            {
                _logger.LogWarning("Tentativo di salvare valore non valido: {0}", _storageSize);
                MessageBox.Show("Lo spazio massimo deve essere maggiore di zero.", 
                    "Valore non valido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _logger.LogDebug("Calcolo bytes per size: {0} {1}", _storageSize, _storageUnit);
                long maxSizeBytes = _storageUnit == "GB" 
                    ? (long)(_storageSize * 1024M * 1024M * 1024M)
                    : (long)(_storageSize * 1024M * 1024M);
                _logger.LogDebug("Dimensione in bytes: {0}", maxSizeBytes);

                _archive.EnforceStorageLimit(maxSizeBytes);
                SaveSettings();
                UpdateStorageStats();
                
                _logger.LogDebug("Impostazioni applicate con successo");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore durante il salvataggio", ex);
                MessageBox.Show($"Errore durante il salvataggio: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _logger.LogDebug("Chiusura dialog");
            _statsTimer?.Stop();
            base.OnClosed(e);
        }
    }

    public class ArchiveSettings
    {
        public decimal MaxStorageSize { get; set; } = 10;
        public string StorageUnit { get; set; } = "MB";
    }
}
