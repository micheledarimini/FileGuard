using System;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using FileGuard.Core.Models;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.FileSystem
{
    public class FileSystemNodeFactory : IFileSystemNodeFactory
    {
        private readonly IStateManager stateManager;
        private readonly Dispatcher dispatcher;

        public FileSystemNodeFactory(IStateManager stateManager, Dispatcher dispatcher)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public FileSystemNode CreateNode(string path, FileSystemInfo? info = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException("File or directory not found", path);
            }

            var nodeState = stateManager.GetOrCreateState(path);
            var isDirectory = info is DirectoryInfo || Directory.Exists(path);

            FileSystemNode node = isDirectory
                ? new DirectoryNode(path, dispatcher, info as DirectoryInfo, stateManager)
                : new FileNode(path, dispatcher);

            // Ripristina lo stato se esiste
            if (nodeState != null)
            {
                Debug.WriteLine($"[RIPRISTINO] {path} - Stato salvato: MonitoringStatus={nodeState.MonitoringStatus}, IsChecked={(nodeState.IsChecked.HasValue ? nodeState.IsChecked.ToString() : "null")}");
                
                node.SetStateDirectly(nodeState.IsChecked, nodeState.MonitoringStatus);

                // Se è una directory, imposta anche lo stato expanded
                if (node is DirectoryNode dirNode)
                {
                    dirNode.IsExpanded = nodeState.IsExpanded;
                }

                Debug.WriteLine($"[RIPRISTINO] {path} - Stato ripristinato: MonitoringStatus={node.MonitoringStatus}, IsChecked={(node.IsChecked.HasValue ? node.IsChecked.ToString() : "null")}");
            }

            return node;
        }

        public FileSystemNode CreateNodeWithParent(string path, FileSystemNode parent)
        {
            var node = CreateNode(path);
            node.Parent = parent;
            return node;
        }

        public FileSystemNode? TryCreateNode(string path)
        {
            try
            {
                return CreateNode(path);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
