using System.Windows.Controls;
using FileGuard.Core.ViewModels;

namespace FileGuard.Core.UI.Components
{
    public partial class MonitoredItemsView : UserControl
    {
        private TreeViewModel? ViewModel => DataContext as TreeViewModel;

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
            if (sender is TreeViewItem item && 
                item.DataContext is FileSystemNodeViewModel node)
            {
                node.IsExpanded = true;
                node.LoadChildren();
            }
        }

        private void TreeViewItem_Collapsed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && 
                item.DataContext is FileSystemNodeViewModel node)
            {
                node.IsExpanded = false;
            }
        }
    }
}
