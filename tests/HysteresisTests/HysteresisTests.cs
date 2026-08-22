using System;
using System.Collections.Generic;
using DellFanManagement.App;
using DellFanManagement.App.FanControllers;
using Xunit;

namespace HysteresisTests
{
    /// <summary>Fake inner controller that records calls instead of touching hardware.</summary>
    public class FakeFanController : FanController
    {
        public List<int> AppliedLevels { get; } = new List<int>();
        public int DisableCalls, EnableCalls, ShutdownCalls;

        public FakeFanController()
        {
            IsAutomaticFanControlDisableSupported = true;
            IsSpecificFanControlSupported = true;
            IsIndividualFanControlSupported = true;
        }

        public override bool DisableAutomaticFanControl() { DisableCalls++; return true; }
        public override bool EnableAutomaticFanControl() { EnableCalls++; return true; }
        public override bool SetFanLevel(FanLevel level, FanIndex fanIndex)
        {
            AppliedLevels.Add((int)level);
            return true;
        }
        public override void Shutdown() { ShutdownCalls++; }
    }

    public class HysteresisFanControllerTests
    {
        [Fact]
        public void FirstLevelChange_IsApplied()
        {
            var inner = new FakeFanController();
            var hyst = new HysteresisFanController(inner, deadband: 1, minInterval: TimeSpan.Zero);
            Assert.True(hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan1));
            Assert.Equal(1, hyst.AppliedChanges);
            Assert.Equal(0, hyst.SuppressedChanges);
        }

        [Fact]
        public void SameLevel_IsSuppressed()
        {
            var inner = new FakeFanController();
            var hyst = new HysteresisFanController(inner, deadband: 1, minInterval: TimeSpan.Zero);
            hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan1);
            Assert.False(hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan1)); // same level
            Assert.Equal(1, hyst.SuppressedChanges);
            Assert.Single(inner.AppliedLevels);
        }

        [Fact]
        public void Deadband_SuppressesSmallChange()
        {
            var inner = new FakeFanController();
            // deadband 2: Off(0) -> Medium(1) = delta 1 < 2 → suppressed
            var hyst = new HysteresisFanController(inner, deadband: 2, minInterval: TimeSpan.Zero);
            hyst.SetFanLevel(FanLevel.Off, FanIndex.Fan1);
            Assert.False(hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan1));
            Assert.Equal(1, hyst.SuppressedChanges);
            Assert.Single(inner.AppliedLevels);
        }

        [Fact]
        public void LargeChange_IsApplied()
        {
            var inner = new FakeFanController();
            // deadband 1: Off(0) -> High(2) = delta 2 >= 1 → applied
            var hyst = new HysteresisFanController(inner, deadband: 1, minInterval: TimeSpan.Zero);
            hyst.SetFanLevel(FanLevel.Off, FanIndex.Fan1);
            Assert.True(hyst.SetFanLevel(FanLevel.High, FanIndex.Fan1));
            Assert.Equal(2, inner.AppliedLevels.Count);
        }

        [Fact]
        public void Debounce_BlocksRapidChange()
        {
            var inner = new FakeFanController();
            // minInterval 1 hour → second change within the hour is suppressed
            var hyst = new HysteresisFanController(inner, deadband: 1, minInterval: TimeSpan.FromHours(1));
            hyst.SetFanLevel(FanLevel.Off, FanIndex.Fan1);
            Assert.False(hyst.SetFanLevel(FanLevel.High, FanIndex.Fan1));
            Assert.Single(inner.AppliedLevels);
        }

        [Fact]
        public void DifferentFans_AreTrackedIndependently()
        {
            var inner = new FakeFanController();
            var hyst = new HysteresisFanController(inner, deadband: 1, minInterval: TimeSpan.Zero);
            hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan1);
            // Fan2 has no history → applied even at same numeric level
            Assert.True(hyst.SetFanLevel(FanLevel.Medium, FanIndex.Fan2));
            Assert.Equal(2, inner.AppliedLevels.Count);
        }

        [Fact]
        public void InnerCapabilities_AreForwarded()
        {
            var inner = new FakeFanController();
            var hyst = new HysteresisFanController(inner);
            Assert.True(hyst.IsSpecificFanControlSupported);
            Assert.True(hyst.IsIndividualFanControlSupported);
        }
    }
}
