using System;

namespace FileGuard.Core.Models
{
    public class FileChangedEventArgs
    {
        public string Path { get; }
        public string Type { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public FileChangedEventArgs(string path, string type, string description)
        {
            Path = path;
            Type = type;
            Description = description;
            Timestamp = DateTime.Now;
        }

        public FileChangedEventArgs(string path, string type, string description, DateTime timestamp)
        {
            Path = path;
            Type = type;
            Description = description;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} - {Type} - {Path} - {Description}";
        }
    }
}
