using System;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;

namespace FileGuard.Core.Models
{
    public class FileNode : FileSystemNode
    {
        public override bool IsDirectory => false;
        public long Size { get; private set; }
        public string SizeDisplay => $"{Size / 1024.0:F2} KB";
        public DateTime CreationTime { get; private set; }

        public FileNode(string path, Dispatcher? dispatcher = null, FileInfo? info = null) 
            : base(path, dispatcher)
        {
            UpdateFileInfo(info);
        }

        public void UpdateFileInfo(FileInfo? info = null)
        {
            try
            {
                if (info != null)
                {
                    Size = info.Length;
                    CreationTime = info.CreationTime;
                }
                else if (File.Exists(Path))
                {
                    var fileInfo = new FileInfo(Path);
                    Size = fileInfo.Length;
                    CreationTime = fileInfo.CreationTime;
                }
                else
                {
                    Size = 0;
                    CreationTime = DateTime.MinValue;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Errore nell'accesso al file {Path}: {ex.Message}");
                Size = 0;
                CreationTime = DateTime.MinValue;
            }
        }
    }
}
