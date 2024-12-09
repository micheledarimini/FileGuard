using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Diagnostics;

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
        public Dictionary<string, NodeState> ChildStates { get; set; }

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
            var oldState = IsChecked;
            var oldStatus = MonitoringStatus;

            IsChecked = newState;
            MonitoringStatus = newState == true ? MonitoringStatus.FullyMonitored :
                             newState == false ? MonitoringStatus.NotMonitored :
                             MonitoringStatus.PartiallyMonitored;

            if (oldState != newState || oldStatus != MonitoringStatus)
            {
                Trace.WriteLine($"[NodeState] PropagateState: {Path} => IsChecked: {oldState}->{newState}, Status: {oldStatus}->{MonitoringStatus}");
            }

            if (updateChildren)
            {
                foreach (var child in ChildStates.Values)
                {
                    child.PropagateState(newState);
                }
                Trace.WriteLine($"[NodeState] PropagateState: {Path} => Propagato a {ChildStates.Count} figli");
            }
            IsDirty = true;
        }

        public void UpdateFromChildren()
        {
            if (!ChildStates.Any()) return;

            var oldState = IsChecked;
            var oldStatus = MonitoringStatus;

            var allChecked = ChildStates.Values.All(c => c.IsChecked == true);
            var allUnchecked = ChildStates.Values.All(c => c.IsChecked == false);

            IsChecked = allChecked ? true :
                       allUnchecked ? false : null;

            MonitoringStatus = IsChecked == true ? MonitoringStatus.FullyMonitored :
                             IsChecked == false ? MonitoringStatus.NotMonitored :
                             MonitoringStatus.PartiallyMonitored;

            if (oldState != IsChecked || oldStatus != MonitoringStatus)
            {
                Trace.WriteLine($"[NodeState] UpdateFromChildren: {Path} => IsChecked: {oldState}->{IsChecked}, Status: {oldStatus}->{MonitoringStatus}");
                Trace.WriteLine($"[NodeState] UpdateFromChildren: {Path} => AllChecked: {allChecked}, AllUnchecked: {allUnchecked}, ChildCount: {ChildStates.Count}");
            }

            IsDirty = true;
        }

        public void ValidateHierarchy()
        {
            foreach (var child in ChildStates.Values)
            {
                var oldParent = child.ParentPath;
                child.ParentPath = Path;
                
                if (oldParent != Path)
                {
                    Trace.WriteLine($"[NodeState] ValidateHierarchy: {child.Path} => Parent: {oldParent}->{Path}");
                }
                
                child.ValidateHierarchy();
            }
        }
    }
}
