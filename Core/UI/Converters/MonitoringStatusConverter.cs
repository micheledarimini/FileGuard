using System;
using System.Globalization;
using System.Windows.Data;
using FileGuard.Core.Models;

namespace FileGuard.Core.UI.Converters
{
    public class MonitoringStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MonitoringStatus status)
            {
                return status == MonitoringStatus.FullyMonitored;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMonitored)
            {
                return isMonitored ? MonitoringStatus.FullyMonitored : MonitoringStatus.NotMonitored;
            }
            return MonitoringStatus.NotMonitored;
        }
    }
}
