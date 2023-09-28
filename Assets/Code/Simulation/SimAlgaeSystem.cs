using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.World;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, -49)] // after phosphorus system
    public sealed class SimAlgaeSystem : SharedStateSystemBehaviour<SimAlgaeState, SimPhosphorusState, SimGridState> {
        // does it make sense to define events in the system or the state?

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.Timer.HasAdvanced()) {
                // for each tile that had a phosphorus change:
                foreach (int tileIndex in m_StateB.Phosphorus.Changes.AffectedTiles) {
                    // check if tile is water
                    ushort flags = m_StateB.Phosphorus.Info[tileIndex].Flags;
                    bool isWater = (flags & (ushort) TerrainFlags.IsWater) != 0;
                    if (!isWater) continue;
                    // get phosphorus count from tile
                    int phosphorusCount = m_StateB.Phosphorus.CurrentState()[tileIndex].Count;
                    // update GrowingTiles
                    if (phosphorusCount >= AlgaeSim.MinPForAlgaeGrowth) {
                        m_StateA.Algae.GrowingTiles.Add(tileIndex);
                    } else {
                        m_StateA.Algae.GrowingTiles.Remove(tileIndex);
                    }
                }
                // Grow algae
                foreach (int tile in m_StateA.Algae.GrowingTiles) {
                    ref float algaeGrowth = ref m_StateA.Algae.State[tile].PercentAlgae;
                    // trigger algae events
                    // TODO: don't dispatch events if algae has already peaked
                    if (!m_StateA.Algae.State[tile].HasPeaked) {
                        DispatchGrowthEvent(algaeGrowth, tile, ref m_StateA.Algae);
                    }
                    // increment by step
                    algaeGrowth += AlgaeSim.AlgaeGrowthIncrement;
                }

                // Decay algae
                foreach (int tile in m_StateA.Algae.BloomedTiles) {
                    if (m_StateA.Algae.GrowingTiles.Contains(tile)) continue;
                    ref float algaeGrowth = ref m_StateA.Algae.State[tile].PercentAlgae;
                    algaeGrowth -= AlgaeSim.AlgaeGrowthIncrement;
                    if (algaeGrowth <= 0) {
                        m_StateA.Algae.BloomedTiles.Remove(tile);
                    }
                }
            }
        }

        private void DispatchGrowthEvent (float currentGrowth, int tileIndex, ref AlgaeBuffers algaeBuffers) {
            ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaeGrew, tileIndex);
            if (currentGrowth <= 0) {
                ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaeFormed, tileIndex);
                algaeBuffers.BloomedTiles.Add(tileIndex);
                InstantiateAlgae(m_StateC, tileIndex);
            } else if (currentGrowth >= 1) {
                ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaePeaked, tileIndex);
                algaeBuffers.PeakingTiles.Add(tileIndex); // flag any peaking tiles for alert system(s)
                algaeBuffers.State[tileIndex].HasPeaked = true;
            }
        }

        /// <summary>
        /// Temporary solution for instantiating an algae game object
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="tileIndex"></param>
        private void InstantiateAlgae (SimGridState grid, int tileIndex) {
            // TODO: grow algae object based on growth state?
            // associate with 
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            // Vector3 worldPos = HexVector.ToWorld(tileIndex, grid.Terrain.Height[tileIndex], ZavalaGame.SimWorld.WorldSpace);
            worldPos.y += 0.01f;
            GameObject newAlgae = Instantiate(m_StateA.AlgaePrefab, worldPos, m_StateA.AlgaePrefab.transform.rotation);
        }
    }
}