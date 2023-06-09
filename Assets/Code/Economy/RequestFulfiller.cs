using System;
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    public sealed class RequestFulfiller : BatchedComponent {
        [NonSerialized] public ResourceSupplier Source;
        [NonSerialized] public ResourceBlock Carrying;
        [NonSerialized] public ResourceRequester Target;

        // target positions
        [NonSerialized] public int SourceTileIndex;
        [NonSerialized] public int TargetTileIndex;
        [NonSerialized] public Vector3 SourceWorldPos;
        [NonSerialized] public Vector3 TargetWorldPos;
    }

    static public class FulfillerUtility {
        static public void InitializeFulfiller(RequestFulfiller unit, MarketActiveRequestInfo request) {
            unit.Source = request.Supplier;
            unit.Carrying = request.Requested;
            unit.Target = request.Requester;

            unit.TargetWorldPos = unit.Target.transform.position;
            unit.SourceWorldPos = unit.Source.transform.position;

            unit.SourceTileIndex = unit.Source.Position.TileIndex;
            unit.TargetTileIndex = unit.Target.Position.TileIndex;
        }
    }
}