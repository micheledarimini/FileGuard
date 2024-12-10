using System.Windows.Controls;

namespace FileGuard.Core.UI.Components
{
    public partial class StatisticsPanel : UserControl
    {
        public StatisticsPanel()
        {
            InitializeComponent();
            DataContext = new ViewModels.StatisticsViewModel();
        }
    }
}
