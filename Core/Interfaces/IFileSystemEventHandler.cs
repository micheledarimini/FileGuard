using System;
using System.IO;

namespace FileGuard.Core.Interfaces
{
    public interface IFileSystemEventHandler
    {
        /// <summary>
        /// Evento scatenato quando un evento del filesystem è stato processato e validato
        /// </summary>
        event EventHandler<FileSystemEventArgs> FileSystemChanged;

        /// <summary>
        /// Gestisce un evento del filesystem applicando debouncing e validazione
        /// </summary>
        /// <param name="sender">Sorgente dell'evento</param>
        /// <param name="e">Argomenti dell'evento del filesystem</param>
        void HandleFileSystemEvent(object sender, FileSystemEventArgs e);
    }
}
