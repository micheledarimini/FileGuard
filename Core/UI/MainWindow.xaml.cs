using System;
using System.Windows;
using System.IO;
using FileGuard.Core.ViewModels;
using FileGuard.Core.UI.Components;

namespace FileGuard.Core.UI
{
    public partial class MainWindow : Window
    {
        private readonly TreeViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            string? assemblyPath = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (assemblyPath == null)
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

            string settingsPath = Path.Combine(assemblyPath, "settings.json");
            string statePath = Path.Combine(assemblyPath, "state.json");
            
            var config = new TreeViewModelConfig(
                settingsPath,
                statePath,
                5  // maxDepth
            );
            
            _viewModel = new TreeViewModel(config);
            DataContext = _viewModel;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleziona una cartella da monitorare",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.AddFolder(dialog.SelectedPath);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedNode != null)
            {
                var popup = new DeleteConfirmationPopup(_viewModel.SelectedNode.Path);
                popup.DeleteConfirmed += (s, confirmed) =>
                {
                    if (confirmed)
                    {
                        _viewModel.RemoveFolder(_viewModel.SelectedNode);
                    }
                };
                popup.ShowDialog();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.SaveState();
        }
    }
}
