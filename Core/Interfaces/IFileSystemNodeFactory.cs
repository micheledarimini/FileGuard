using System.IO;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface IFileSystemNodeFactory
    {
        FileSystemNode CreateNode(string path, FileSystemInfo? info = null);
        FileSystemNode CreateNodeWithParent(string path, FileSystemNode parent);
        FileSystemNode? TryCreateNode(string path);
    }
}
