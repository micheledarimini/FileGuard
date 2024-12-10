using System;

namespace FileGuard.Core.Models
{
    public class FileChangeInfo
    {
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
