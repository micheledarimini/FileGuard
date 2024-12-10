using System;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using FileGuard.Core.Interfaces;

namespace FileGuard.Core.Models
{
    public class FileNode : FileSystemNode
    {
        private readonly IStateManager? stateManager;
        private readonly ISelectionManager selectionManager;
        public override bool IsDirectory => false;
        public long Size { get; private set; }
        public string SizeDisplay => $"{Size / 1024.0:F2} KB";
        public DateTime CreationTime { get; private set; }

        public FileNode(string path, Dispatcher? dispatcher = null, FileInfo? info = null, 
                       IStateManager? stateManager = null, ISelectionManager? selectionManager = null) 
            : base(path, dispatcher)
        {
            this.stateManager = stateManager;
            this.selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));
            
            UpdateFileInfo(info);

            if (stateManager != null)
            {
                var state = stateManager.GetOrCreateState(path);
                if (state != null)
                {
                    Trace.WriteLine($"[FileNode] Costruttore: {path} => IsChecked: {state.IsChecked}, Status: {state.MonitoringStatus}");
                    SetStateDirectly(state.IsChecked, state.MonitoringStatus);
                }
            }

            // Sottoscrizione agli eventi di selezione
            this.selectionManager.SelectionChanged += HandleSelectionChanged;
        }

        private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.Path.Equals(Path, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine($"[FileNode] SelectionChanged: {Path} => IsChecked: {e.IsChecked}, Status: {e.Status}");
                SetStateDirectly(e.IsChecked, e.Status);
            }
        }

        protected override void PropagateStateToChildren(bool state)
        {
            Trace.WriteLine($"[FileNode] PropagateStateToChildren: {Path} => {state}");
            selectionManager.SetNodeSelection(Path, state);
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
                Trace.WriteLine($"[FileNode] Errore nell'accesso al file {Path}: {ex.Message}");
                Size = 0;
                CreationTime = DateTime.MinValue;
            }
        }
    }
}
