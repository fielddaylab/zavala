using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Actors {
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class ActorPhosphorusGeneratorSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, ResourceStorage, OccupiesTile> {
        public override bool HasWork() {
            if (base.HasWork()) {
                // disable phosphorus generation for tutorial
                return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
            }

            return false;
        }

        public override void ProcessWork(float deltaTime) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();

            foreach(var componentGroup in m_Components) {
                if (componentGroup.ComponentA.HasAdvanced()) {
                    SimPhospohorusUtility.GenerateProportionalPhosphorus(
                        phosphorus,
                        componentGroup.ComponentC.TileIndex,
                        componentGroup.Primary,
                        componentGroup.ComponentB.Current,
                        RunoffParams.SittingManureRunoffProportion,
                        RunoffParams.MFertilizerRunoffProportion,
                        RunoffParams.DFertilizerRunoffProportion
                        );
                }
            }
        }
    }
}