using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FileGuard.Core.Models;
using System.Media;

namespace FileGuard.Core.UI.Components
{
    public partial class NewFilePopup : Window
    {
        private readonly DispatcherTimer timer;
        private int remainingSeconds = 30;
        private readonly string filePath;
        private readonly FileNode fileNode;
        private readonly Action<FileNode> onMonitorFile;

        public NewFilePopup(string filePath, FileNode fileNode, Action<FileNode> onMonitorFile)
        {
            InitializeComponent();
            
            this.filePath = filePath;
            this.fileNode = fileNode;
            this.onMonitorFile = onMonitorFile;

            // Inizializza il timer
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick!;

            // Posiziona la finestra in basso a destra
            PositionWindow();

            // Imposta i dati del file
            SetFileInfo();

            // Imposta il testo iniziale del timer
            TimerText.Text = $"Chiusura automatica in {remainingSeconds} secondi";
            TimerText.Visibility = Visibility.Visible;

            // Riproduci il suono di notifica
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nella riproduzione del suono: {ex}");
            }

            // Avvia il timer e l'animazione
            timer.Start();
            AnimatePopup();
        }

        private void SetFileInfo()
        {
            try
            {
                // Nome file
                FileNameText.Text = Path.GetFileName(filePath);

                // Percorso
                FilePathText.Text = Path.GetDirectoryName(filePath);

                // Data e dimensione
                var size = GetFormattedFileSize(fileNode.Size);
                FileDateSizeText.Text = $"{fileNode.CreationTime:dd MMM yyyy, HH:mm} - {size}";

                // Imposta l'icona (nascosta ma mantenuta per compatibilità)
                SetFileIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'impostazione delle info del file: {ex}");
            }
        }

        private void SetFileIcon()
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                var iconPath = $"/FileGuard;component/Resources/Icons/{extension.TrimStart('.')}.png";
                
                try
                {
                    var uri = new Uri(iconPath, UriKind.Relative);
                    FileIcon.Source = new BitmapImage(uri);
                }
                catch
                {
                    // Se l'icona specifica non esiste, usa l'icona generica
                    FileIcon.Source = new BitmapImage(new Uri("/FileGuard;component/Resources/Icons/file.png", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'impostazione dell'icona: {ex}");
            }
        }

        private string GetFormattedFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.#} {sizes[order]}";
        }

        private void PositionWindow()
        {
            // Posiziona la finestra in basso a destra
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
        }

        private void AnimatePopup()
        {
            // Animazione di slide-up
            Opacity = 0;
            var animation = new DoubleAnimation
            {
                From = Top + 20,
                To = Top,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            BeginAnimation(TopProperty, animation);
            BeginAnimation(OpacityProperty, fadeAnimation);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;
            
            if (remainingSeconds > 0)
            {
                // Aggiorna il testo del timer
                TimerText.Text = $"Chiusura automatica in {remainingSeconds} secondi";
            }
            else
            {
                ClosePopup();
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                onMonitorFile?.Invoke(fileNode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'attivazione del monitoraggio: {ex}");
            }
            finally
            {
                ClosePopup();
            }
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePopup();
        }

        private void ClosePopup()
        {
            timer.Stop();

            // Animazione di chiusura
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            animation.Completed += (s, _) => Close();
            BeginAnimation(OpacityProperty, animation);
        }

        public static void Show(string filePath, FileNode fileNode, Action<FileNode> onMonitorFile)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var popup = new NewFilePopup(filePath, fileNode, onMonitorFile);
                popup.Show();
            });
        }
    }
}
