using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using Zavala.Scripting;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, -50, ZavalaGame.SimulationUpdateMask)]
    public sealed class SimPhosphorusSystem : SharedStateSystemBehaviour<SimPhosphorusState, SimGridState> {
        private Action m_ResetCallback;
        
        #region Work

        public override void ProcessWork(float deltaTime) {
            bool advanced = m_StateA.Timer.Advance(deltaTime, ZavalaGame.SimTime);
            if (advanced) {
                using (Profiling.Time("phosphorus sim tick")) {
                    PhosphorusSim.Tick(m_StateA.Phosphorus.Info, m_StateA.Phosphorus.CurrentState(), m_StateA.Phosphorus.NextState(), m_StateB.HexSize, m_StateB.SimulationRegion, m_StateB.Random, m_StateA.Phosphorus.Changes);
                    m_StateA.Phosphorus.StateIndex = 1 - m_StateA.Phosphorus.StateIndex;
                    PhosphorusSim.TickPhosphorusHistory(m_StateA.HistoryPerRegion, m_StateB.Regions, (int) m_StateB.RegionCount);
                }

                RegionUnlockState unlockState = Game.SharedState.Get<RegionUnlockState>();
                RegionAgeState ageState = Game.SharedState.Get<RegionAgeState>();
                    
                Game.SharedState.Get<WinLossState>().CheckTimer = true;

                RegionUnlockUtility.RegisterPTimerAdvanced(unlockState);
                RegionAgeUtility.RegisterPTimerAdvanced(ageState);
                Game.Events.Queue(GameEvents.SimPhosphorusTick);
            }

            if (m_StateA.Phosphorus.Changes.AffectedTiles.Count > 0) {
                foreach (var region in m_StateA.Phosphorus.Changes.AffectedRegions) {
                    m_StateA.UpdatedPhosphorusRegionMask |= 1u << region;
                }
            }

            GameLoop.QueuePreUpdate(m_ResetCallback);
        }

        private void ResetDirtyDataFlags() {
            m_StateA.Phosphorus.Changes.Clear();
            m_StateA.UpdatedPhosphorusRegionMask = 0;
            m_StateA.Timer.ClearAdvancedOnFrame();
            m_StateB.UpdatedRegions.Clear();
        }

        #endregion // Work

        #region Callbacks

        public override void Initialize() {
            base.Initialize();

            m_ResetCallback = ResetDirtyDataFlags;
        }

        public override void Shutdown() {
            base.Shutdown();
        }

        #endregion // Callbacks
    }
}