using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows;

namespace FileGuard.Core.UI.Converters
{
    public class IconConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapSource> _iconCache = new();
        private static readonly BitmapSource _fallbackIcon;
        
        static IconConverter()
        {
            // Create a 16x16 gray bitmap as fallback
            var width = 16;
            var height = 16;
            var stride = width * 4; // 4 bytes per pixel (BGRA)
            var pixels = new byte[height * stride];
            
            // Fill with gray color (R=128, G=128, B=128, A=255)
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 128;     // B
                pixels[i + 1] = 128; // G
                pixels[i + 2] = 128; // R
                pixels[i + 3] = 255; // A
            }
            
            _fallbackIcon = BitmapSource.Create(
                width, height,
                96, 96, // DPI
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);
            _fallbackIcon.Freeze(); // Make it immutable for better performance
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, 
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrEmpty(path))
                return _fallbackIcon;

            try 
            {
                return _iconCache.GetOrAdd(path, p => {
                    var info = new SHFILEINFO();
                    uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
                    uint attributes = System.IO.Directory.Exists(p) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                    
                    IntPtr result = SHGetFileInfo(p, attributes, ref info, (uint)Marshal.SizeOf(info), flags);
                    
                    if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                        return _fallbackIcon;

                    try
                    {
                        var icon = Imaging.CreateBitmapSourceFromHIcon(
                            info.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                            
                        if (icon != null)
                        {
                            icon.Freeze(); // Make it immutable for better performance
                            return icon;
                        }
                        return _fallbackIcon;
                    }
                    finally
                    {
                        if (info.hIcon != IntPtr.Zero)
                            DestroyIcon(info.hIcon);
                    }
                });
            }
            catch (Exception)
            {
                return _fallbackIcon;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
