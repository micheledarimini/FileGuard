using System.Windows.Controls;
using FileGuard.Core.ViewModels;

namespace FileGuard.Core.UI.Components
{
    public partial class MonitoredItemsView : UserControl
    {
        private TreeViewModel? ViewModel => DataContext as TreeViewModel;
        private bool isInternalUpdate = false;

        public MonitoredItemsView()
        {
            InitializeComponent();
        }

        private void FolderTreeView_SelectedItemChanged(object sender, 
            System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel != null && e.NewValue is FileSystemNodeViewModel node)
            {
                ViewModel.SelectedNode = node;
            }
        }

        private void TreeViewItem_Expanded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isInternalUpdate) return;

            if (sender is TreeViewItem item && 
                item.DataContext is FileSystemNodeViewModel node)
            {
                try
                {
                    isInternalUpdate = true;
                    node.IsExpanded = true;
                    node.LoadChildren();
                }
                finally
                {
                    isInternalUpdate = false;
                }
            }
        }

        private void TreeViewItem_Collapsed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isInternalUpdate) return;

            if (sender is TreeViewItem item && 
                item.DataContext is FileSystemNodeViewModel node)
            {
                try
                {
                    isInternalUpdate = true;
                    node.IsExpanded = false;
                }
                finally
                {
                    isInternalUpdate = false;
                }
            }
        }
    }
}
