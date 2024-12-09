using System;
using System.IO;
using System.Windows.Threading;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemNodeFactory
    {
        private readonly Dispatcher dispatcher;
        private readonly IStateManager stateManager;
        private readonly ISelectionManager selectionManager;

        public FileSystemNodeFactory(Dispatcher dispatcher, IStateManager stateManager, ISelectionManager selectionManager)
        {
            this.dispatcher = dispatcher;
            this.stateManager = stateManager;
            this.selectionManager = selectionManager;
        }

        public FileSystemNode CreateNode(string path, FileSystemInfo info)
        {
            if (info is DirectoryInfo)
            {
                return new DirectoryNode(path, dispatcher, info as DirectoryInfo, stateManager, selectionManager);
            }
            
            return new FileNode(path, dispatcher);
        }
    }
}
