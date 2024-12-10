using System;
using System.Windows;

namespace FileGuard.Core.UI.Components
{
    public partial class DeleteConfirmationPopup : Window
    {
        public event EventHandler<bool>? DeleteConfirmed;

        public DeleteConfirmationPopup(string folderPath)
        {
            InitializeComponent();
            FolderPathText.Text = folderPath;

            // Centra la finestra rispetto alla finestra principale
            if (Application.Current.MainWindow != null)
            {
                this.Owner = Application.Current.MainWindow;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed?.Invoke(this, true);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed?.Invoke(this, false);
            this.Close();
        }
    }
}
