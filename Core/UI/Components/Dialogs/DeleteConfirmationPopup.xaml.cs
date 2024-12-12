using System;
using System.Windows;

namespace FileGuard.Core.UI.Components.Dialogs
{
    public partial class DeleteConfirmationPopup : Window
    {
        public event EventHandler<bool>? DeleteConfirmed;
        public string Message { get; }

        public DeleteConfirmationPopup(string path)
        {
            InitializeComponent();
            Message = $"Sei sicuro di voler rimuovere '{path}' dal monitoraggio?";
            DataContext = this;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed?.Invoke(this, true);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed?.Invoke(this, false);
            Close();
        }
    }
}
