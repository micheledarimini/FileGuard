using System.Windows;
using System.Windows.Controls;
using FileGuard.Core.ViewModels;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.UI.Components.Monitoring
{
    public partial class MonitoredItemsView : UserControl
    {
        public MonitoredItemsView()
        {
            InitializeComponent();
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is TreeViewModel viewModel)
            {
                viewModel.SelectedNode = e.NewValue as ITreeNode;
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is FileSystemNodeViewModel node)
            {
                node.IsExpanded = true;
            }
        }

        private void TreeViewItem_Collapsed(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is FileSystemNodeViewModel node)
            {
                node.IsExpanded = false;
            }
        }
    }
}
