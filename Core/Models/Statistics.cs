namespace FileGuard.Core.Models
{
    public class Statistics
    {
        public int MonitoredFolders { get; set; }
        public int MonitoredFiles { get; set; }
        public long BackupSpaceBytes { get; set; }
        public int TodayEvents { get; set; }
        public System.DateTime LastUpdate { get; set; }

        public string FormatBackupSpace()
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (BackupSpaceBytes >= GB)
                return $"{BackupSpaceBytes / (double)GB:F1} GB";
            if (BackupSpaceBytes >= MB)
                return $"{BackupSpaceBytes / (double)MB:F1} MB";
            if (BackupSpaceBytes >= KB)
                return $"{BackupSpaceBytes / (double)KB:F1} KB";
            
            return $"{BackupSpaceBytes} B";
        }
    }
}
