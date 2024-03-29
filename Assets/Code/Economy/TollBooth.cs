
using UnityEngine;
using FieldDay.Components;
using Zavala.Roads;
using FieldDay;

namespace Zavala.Economy {
    public sealed class TollBooth : BatchedComponent {
        public OccupiesTile TileA;
        public OccupiesTile TileB;
        public TileDirection AToBDirection;
        public RoadInstanceController RoadA;
        public RoadInstanceController RoadB;

        protected override void OnEnable() {
            base.OnEnable();

            SetDirection();
            RoadUtility.RegisterSource(TileA, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterSource(TileB, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterDestination(TileA, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterDestination(TileB, RoadDestinationMask.Tollbooth);

            RoadUtility.RegisterFixedRoad(RoadA);
            RoadUtility.RegisterFixedRoad(RoadB);
        }

        protected override void OnDisable() {
            if (Game.IsShuttingDown || !Frame.IsLoadingOrLoaded(this)) {
                return;
            }

            RoadUtility.DeregisterSource(TileA);
            RoadUtility.DeregisterSource(TileB);
            RoadUtility.DeregisterDestination(TileA);
            RoadUtility.DeregisterDestination(TileB);
            RoadUtility.DeregisterFixedRoad(RoadA);
            RoadUtility.DeregisterFixedRoad(RoadB);

            RoadA.gameObject.SetActive(false);
            RoadB.gameObject.SetActive(false);

            base.OnDisable();
        }

        private void SetDirection() {
            AToBDirection = HexVector.Direction(TileA.TileVector, TileB.TileVector);
            RoadNetwork network = ZavalaGame.SharedState.Get<RoadNetwork>();
            network.Roads.Info[TileA.TileIndex].FlowMask[AToBDirection] = true;
            network.Roads.Info[TileB.TileIndex].FlowMask[AToBDirection.Reverse()] = true;
            network.Roads.Info[TileA.TileIndex].PreserveFlow = AToBDirection;
            network.Roads.Info[TileB.TileIndex].PreserveFlow = AToBDirection.Reverse();
            network.Roads.Info[TileA.TileIndex].Flags |= RoadFlags.IsTollbooth;
            network.Roads.Info[TileB.TileIndex].Flags |= RoadFlags.IsTollbooth;

            RoadA.transform.SetPositionAndRotation(TileA.transform.position, Quaternion.identity);
            RoadB.transform.SetPositionAndRotation(TileB.transform.position, Quaternion.identity);

            RoadA.gameObject.SetActive(true);
            RoadB.gameObject.SetActive(true);
        }
    }


}