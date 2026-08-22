using System;
using System.Collections.Generic;

namespace DellFanManagement.App.FanControllers
{
    /// <summary>
    /// Wraps a <see cref="FanController"/> with hysteresis (deadband) logic.
    ///
    /// Community roadmap #1: users of similar tools (FanControl, 20k★) report
    /// hysteresis as "confusing" or "doesn't work". This implementation is
    /// explicit and predictable:
    ///   - A level change is only applied when the requested level differs
    ///     from the current level by more than <see cref="Deadband"/>.
    ///   - Repeated equal-level requests are suppressed (no redundant writes).
    ///   - A <see cref="MinInterval"/> debounce prevents rapid oscillation.
    ///
    /// This is a pure-logic wrapper: it holds state and decides, then delegates
    /// the actual hardware write to the inner controller. Unit-testable without
    /// hardware.
    /// </summary>
    public class HysteresisFanController : FanController
    {
        private readonly FanController _inner;
        private readonly int _deadband;
        private readonly TimeSpan _minInterval;
        private readonly Dictionary<FanIndex, int> _currentLevels = new Dictionary<FanIndex, int>();
        private readonly Dictionary<FanIndex, DateTime> _lastChanges = new Dictionary<FanIndex, DateTime>();

        /// <summary>Total number of level-change requests accepted (applied).</summary>
        public int AppliedChanges { get; private set; }

        /// <summary>Total number of level-change requests suppressed by hysteresis.</summary>
        public int SuppressedChanges { get; private set; }

        /// <param name="inner">The real fan controller to delegate to.</param>
        /// <param name="deadband">Minimum |requested - current| delta required to apply a change (>= 1).</param>
        /// <param name="minInterval">Minimum time between actual level changes per fan.</param>
        public HysteresisFanController(FanController inner, int deadband = 1, TimeSpan? minInterval = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _deadband = Math.Max(1, deadband);
            _minInterval = minInterval ?? TimeSpan.FromSeconds(5);

            IsAutomaticFanControlDisableSupported = _inner.IsAutomaticFanControlDisableSupported;
            IsSpecificFanControlSupported = _inner.IsSpecificFanControlSupported;
            IsIndividualFanControlSupported = _inner.IsIndividualFanControlSupported;
        }

        /// <summary>
        /// Apply a fan level with hysteresis. Returns true if the hardware write
        /// happened, false if it was suppressed by the deadband or debounce.
        /// </summary>
        public override bool SetFanLevel(FanLevel level, FanIndex fanIndex)
        {
            int requested = (int)level;

            // Suppress repeated equal-level requests.
            if (_currentLevels.TryGetValue(fanIndex, out int current) && Math.Abs(requested - current) < _deadband)
            {
                SuppressedChanges++;
                return false;
            }

            // Debounce: don't allow a second change too soon after the last one.
            if (_lastChanges.TryGetValue(fanIndex, out DateTime last))
            {
                if (DateTime.UtcNow - last < _minInterval)
                {
                    SuppressedChanges++;
                    return false;
                }
            }

            bool ok = _inner.SetFanLevel(level, fanIndex);
            if (ok)
            {
                _currentLevels[fanIndex] = requested;
                _lastChanges[fanIndex] = DateTime.UtcNow;
                AppliedChanges++;
            }
            return ok;
        }

        public override bool DisableAutomaticFanControl() => _inner.DisableAutomaticFanControl();

        public override bool EnableAutomaticFanControl() => _inner.EnableAutomaticFanControl();

        public override void Shutdown() => _inner.Shutdown();
    }
}
