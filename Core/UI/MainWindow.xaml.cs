using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FileGuard.Core.ViewModels;
using FileGuard.Core.Models;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using TreeView = System.Windows.Controls.TreeView;
using TreeViewItem = System.Windows.Controls.TreeViewItem;
using System.Diagnostics;

namespace FileGuard.Core.UI
{
    public partial class MainWindow : Window
    {
        private readonly TreeViewModel viewModel;
        private readonly string settingsPath;
        private bool isClosing;

        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine("Inizializzazione MainWindow");

            settingsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "fileguard_settings.json"
            );

            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents"
            );

            var config = new TreeViewModelConfig(
                settingsPath: settingsPath,
                defaultMonitorPath: defaultPath,
                maxChangeHistoryItems: 100,
                autoLoadMonitoredFolders: true
            );

            viewModel = new TreeViewModel(config);
            DataContext = viewModel;

            Debug.WriteLine("MainWindow inizializzata");

            // Aggiungi handler per il debug dei checkbox
            if (Debugger.IsAttached)
            {
                folderTreeView.AddHandler(
                    System.Windows.Controls.CheckBox.CheckedEvent,
                    new RoutedEventHandler(CheckBox_StateChanged)
                );
                folderTreeView.AddHandler(
                    System.Windows.Controls.CheckBox.UncheckedEvent,
                    new RoutedEventHandler(CheckBox_StateChanged)
                );
                folderTreeView.AddHandler(
                    System.Windows.Controls.CheckBox.IndeterminateEvent,
                    new RoutedEventHandler(CheckBox_StateChanged)
                );
            }
        }

        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.CheckBox checkBox && 
                checkBox.Tag is FileSystemNode node)
            {
                Debug.WriteLine($"Checkbox stato cambiato - Nodo: {node.Path}, Stato: {checkBox.IsChecked}, MonitoringStatus: {node.MonitoringStatus}");
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dialog = new WinForms.FolderBrowserDialog
                {
                    Description = "Seleziona una cartella da monitorare",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    Debug.WriteLine($"Cartella selezionata: {dialog.SelectedPath}");
                    viewModel.AddFolder(dialog.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                LogError("BrowseButton_Click", ex);
                MessageBox.Show(
                    $"Errore durante la selezione della cartella: {ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewModel.SelectedNode != null)
                {
                    Debug.WriteLine($"Rimozione cartella richiesta: {viewModel.SelectedNode.Path}");
                    viewModel.RemoveFolder(viewModel.SelectedNode);
                }
            }
            catch (Exception ex)
            {
                LogError("RemoveButton_Click", ex);
                MessageBox.Show(
                    $"Errore durante la rimozione della cartella: {ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is FileSystemNode node)
                {
                    Debug.WriteLine($"Selezione cambiata: {node.Path}");
                    viewModel.SelectedNode = node;
                }
            }
            catch (Exception ex)
            {
                LogError("FolderTreeView_SelectedItemChanged", ex);
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TreeViewItem item && item.DataContext is DirectoryNode dirNode)
                {
                    Debug.WriteLine($"Nodo espanso: {dirNode.Path}");
                    dirNode.LoadContents();
                    dirNode.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                LogError("TreeViewItem_Expanded", ex);
            }
        }

        private void TreeViewItem_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TreeViewItem item && item.DataContext is DirectoryNode dirNode)
                {
                    Debug.WriteLine($"Nodo collassato: {dirNode.Path}");
                    dirNode.IsExpanded = false;
                }
            }
            catch (Exception ex)
            {
                LogError("TreeViewItem_Collapsed", ex);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isClosing) return;
            isClosing = true;

            try
            {
                Debug.WriteLine("Salvataggio stato applicazione");
                viewModel.SaveState();
            }
            catch (Exception ex)
            {
                LogError("MainWindow_Closing", ex);
                MessageBox.Show(
                    $"Errore durante il salvataggio delle impostazioni: {ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LogError(string context, Exception ex)
        {
            Debug.WriteLine($"Errore in {context}: {ex}");
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fileguard_error.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {context}: {ex}\n"
                );
            }
            catch { }
        }
    }
}
