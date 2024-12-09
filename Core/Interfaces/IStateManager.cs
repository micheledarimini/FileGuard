using System.Collections.Generic;
using System.Threading.Tasks;
using FileGuard.Core.Models;

namespace FileGuard.Core.Interfaces
{
    public interface IStateManager
    {
        void AddMonitoredPath(string path);
        void RemoveMonitoredPath(string path);
        IEnumerable<string> GetMonitoredPaths();
        MonitoringStatus GetMonitoringStatus(string path);
        bool? GetIsChecked(string path);
        bool GetIsExpanded(string path);
        void UpdateNodeState(string path, MonitoringStatus status, bool? isChecked, bool isExpanded);
        void UpdateMonitoringStatus(string path, MonitoringStatus status);
        void UpdateIsExpanded(string path, bool isExpanded);
        NodeState? GetOrCreateState(string path);
        Task SaveStateToDiskAsync();
    }
}
