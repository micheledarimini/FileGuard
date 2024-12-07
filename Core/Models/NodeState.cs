using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FileGuard.Core.Models
{
    public class NodeState
    {
        [JsonPropertyName("monitoringStatus")]
        public MonitoringStatus MonitoringStatus { get; set; }

        [JsonPropertyName("isChecked")] 
        public bool? IsChecked { get; set; }

        [JsonPropertyName("isExpanded")]
        public bool IsExpanded { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("parentPath")]
        public string? ParentPath { get; set; }

        [JsonPropertyName("childStates")]
        public Dictionary<string, NodeState> ChildStates { get; private set; }

        [JsonIgnore]
        public bool IsDirty { get; set; }

        public NodeState()
        {
            MonitoringStatus = MonitoringStatus.NotMonitored;
            IsChecked = false;
            IsExpanded = false;
            Path = null;
            ParentPath = null;
            ChildStates = new Dictionary<string, NodeState>(StringComparer.OrdinalIgnoreCase);
        }

        public void PropagateState(bool? newState, bool updateChildren = true)
        {
            IsChecked = newState;
            MonitoringStatus = newState == true ? MonitoringStatus.FullyMonitored :
                             newState == false ? MonitoringStatus.NotMonitored :
                             MonitoringStatus.PartiallyMonitored;

            if (updateChildren)
            {
                foreach (var child in ChildStates.Values)
                {
                    child.PropagateState(newState);
                }
            }
            IsDirty = true;
        }

        public void UpdateFromChildren()
        {
            if (!ChildStates.Any()) return;

            var allChecked = ChildStates.Values.All(c => c.IsChecked == true);
            var allUnchecked = ChildStates.Values.All(c => c.IsChecked == false);

            IsChecked = allChecked ? true :
                       allUnchecked ? false : null;

            MonitoringStatus = IsChecked == true ? MonitoringStatus.FullyMonitored :
                             IsChecked == false ? MonitoringStatus.NotMonitored :
                             MonitoringStatus.PartiallyMonitored;

            IsDirty = true;
        }

        public void ValidateHierarchy()
        {
            foreach (var child in ChildStates.Values)
            {
                child.ParentPath = Path;
                child.ValidateHierarchy();
            }
        }
    }
}
