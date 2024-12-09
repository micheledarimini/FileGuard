using System.Windows;
using System.Windows.Controls;

namespace FileGuard.Core.UI.Controls
{
    public class IndependentTreeViewItem : TreeViewItem
    {
        protected override void OnExpanded(RoutedEventArgs e)
        {
            // Ferma la propagazione dell'evento
            e.Handled = true;
            base.OnExpanded(e);
        }

        protected override void OnCollapsed(RoutedEventArgs e)
        {
            // Ferma la propagazione dell'evento
            e.Handled = true;
            base.OnCollapsed(e);
        }
    }
}
