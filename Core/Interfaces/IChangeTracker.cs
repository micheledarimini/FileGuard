using System.Collections.ObjectModel;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface IChangeTracker
    {
        ObservableCollection<FileChangedEventArgs> Changes { get; }
        void TrackChange(string path, string type, string description);
        void Clear();
    }
}
