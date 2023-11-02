using System;
using BeauPools;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceStorage), typeof(OccupiesTile))]
    public sealed class ResourceSupplier : BatchedComponent {
        [AutoEnum] public ResourceMask ShippingMask;
        
        [NonSerialized] public OccupiesTile Position;

        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public MarketSupplierPriorityList Priorities;
        [NonSerialized] public bool SoldAtALoss = false;

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Position);

            Priorities.Create();
        }

        protected override void OnEnable() {
            base.OnEnable();

            MarketUtility.RegisterSupplier(this);
            RoadUtility.RegisterSource(Position, (RoadDestinationMask) ShippingMask | RoadDestinationMask.Tollbooth | RoadDestinationMask.Export);
        }

        protected override void OnDisable() {
            MarketUtility.DeregisterSupplier(this);
            RoadUtility.DeregisterSource(Position);

            SoldAtALoss = false;
            Priorities.PrioritizedBuyers.Clear();

            base.OnDisable();
        }
    }
}