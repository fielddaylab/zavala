using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf.Runtime;
using System;
using UnityEngine;
using Zavala.Data;

namespace Zavala.Sim {
    public sealed class SimAlgaeState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject {
        static public readonly StringHash32 Event_AlgaeFormed = "AlgaeState::AlgaeFormed";
        static public readonly StringHash32 Event_AlgaeGrew = "AlgaeState::AlgaeGrew";
        static public readonly StringHash32 Event_AlgaePeaked = "AlgaeState::AlgaePeaked";

        [NonSerialized] public int CurrentMinPForAlgaeGrowth;
        [NonSerialized] public AlgaeBuffers Algae;
        // public GameObject AlgaePrefab;

        [Header("Per-Region")]
        [NonSerialized] public float[] TotalAlgaePerRegion;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault;
            SimGridState gridState = ZavalaGame.SimGrid;
            TotalAlgaePerRegion = new float[RegionInfo.MaxRegions];
            Algae.Create(gridState.HexSize);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref byte* data, ref int remaining, SaveStateChunkConsts consts) {
            // TODO: Implement
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref byte* data, ref int written, int capacity, SaveStateChunkConsts consts) {
            // TODO: Implement
        }
    }
    public class SimAlgaeUtility {
        static public float AddAlgaeToTile(SimAlgaeState algaeState, int tileIndex, float delta, int region) {
            ref float current = ref algaeState.Algae.State[tileIndex].PercentAlgae;
            if (current + delta > 1) {
                delta = 1 - current;
            } else if (current + delta < 0) { //
                delta = -current;
            }
            current += delta;
            //int region = Game.SharedState.Get<SimGridState>().Terrain.Info[tileIndex].RegionIndex;
            RecordAlgaeToRegionTotal(algaeState, region, delta);
            return delta;
        }
        static public float RemoveAlgae(SimAlgaeState algaeState, int tileIndex, float amount, int region) {
            return AddAlgaeToTile(algaeState, tileIndex, -amount, region);
        }

        static public void RecordAlgaeToRegionTotal(SimAlgaeState state, int regionIndex, float amt) {
            state.TotalAlgaePerRegion[regionIndex] += amt;
        }

        [LeafMember("SetAlgaeGrowthThreshold")]
        public static void SetAlgaeGrowthThreshold(int minPForAlgae) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = minPForAlgae;
        }

        [LeafMember("OffsetAlgaeGrowthThreshold")]
        public static void OffsetAlgaeGrowthThreshold(int delta) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault + delta;
        }

    }
}