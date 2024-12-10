using System;
using System.Collections.Generic;
using System.Linq;
using FileGuard.Core.Interfaces;
using FileGuard.Core.Models;
using FileGuard.Core.Logging;

namespace FileGuard.Core.State
{
    public class NodeStateHandler
    {
        private readonly ILogger _logger;
        private bool? _isChecked;
        private MonitoringStatus _status;
        private bool _isUpdating;
        private readonly string _nodePath;

        public bool? IsChecked => _isChecked;
        public MonitoringStatus Status => _status;

        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public NodeStateHandler(string nodePath, ILogger? logger = null)
        {
            _nodePath = nodePath;
            _logger = logger ?? LoggerFactory.GetDefaultLogger();
            _isChecked = false;
            _status = MonitoringStatus.NotMonitored;
        }

        public void UpdateState(bool? newState, MonitoringStatus newStatus, bool propagate = true)
        {
            if (_isUpdating) return;

            try
            {
                _isUpdating = true;
                _logger.LogDebug($"Aggiornamento stato: {_isChecked} -> {newState}, Status: {newStatus}", nameof(NodeStateHandler));

                var stateChanged = _isChecked != newState || _status != newStatus;
                if (stateChanged)
                {
                    _isChecked = newState;
                    _status = newStatus;
                    OnStateChanged(propagate);
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        public void UpdateFromChildren(IEnumerable<ITreeNode> children)
        {
            if (!children.Any())
            {
                UpdateState(false, MonitoringStatus.NotMonitored, false);
                return;
            }

            var selectedCount = children.Count(c => c.IsChecked == true);
            var totalCount = children.Count();
            var hasIndeterminate = children.Any(c => c.IsChecked == null);

            if (hasIndeterminate)
            {
                UpdateState(null, MonitoringStatus.PartiallyMonitored, false);
            }
            else if (selectedCount == totalCount)
            {
                UpdateState(true, MonitoringStatus.FullyMonitored, false);
            }
            else if (selectedCount == 0)
            {
                UpdateState(false, MonitoringStatus.NotMonitored, false);
            }
            else
            {
                UpdateState(null, MonitoringStatus.PartiallyMonitored, false);
            }
        }

        private void OnStateChanged(bool propagate)
        {
            var args = new StateChangedEventArgs
            {
                NewState = _isChecked,
                NewStatus = _status,
                ShouldPropagate = propagate,
                Path = _nodePath
            };

            StateChanged?.Invoke(this, args);
        }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public bool? NewState { get; set; }
        public MonitoringStatus NewStatus { get; set; }
        public bool ShouldPropagate { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}
