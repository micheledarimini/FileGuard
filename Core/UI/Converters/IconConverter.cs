using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace FileGuard.Core.UI.Converters
{
    public class IconConverter : IValueConverter
    {
        private static readonly Geometry folderGeometry = Geometry.Parse("M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z");
        private static readonly Geometry fileGeometry = Geometry.Parse("M13,9V3.5L18.5,9M6,2C4.89,2 4,2.89 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2H6Z");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var path = new Path
            {
                Data = value is bool isDirectory ? (isDirectory ? folderGeometry : fileGeometry) : fileGeometry,
                Fill = Brushes.Gray,
                Stretch = Stretch.Uniform,
                Width = 16,
                Height = 16
            };

            return path;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
