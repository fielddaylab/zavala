using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceRequester))]
    public sealed class ResourcePurchaser : BatchedComponent {
        static public readonly StringHash32 Event_PurchaseMade = "ResourcePurchaser::ResourcePurchased";
        static public readonly StringHash32 Event_PurchaseUnfulfilled = "ResourcePurchaser::PurchaseUnfulfilled";

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock RequestAmount;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock PurchasePrice;

        [NonSerialized] public ResourceRequester Request;
        [NonSerialized] public ResourceStorage Storage;

        // TODO: okay to define these methods here and call them elsewhere or should they be defined in ResourcePurchaserSystem?
        public void ChangeRequestAmount(ResourceId resource, int change) {
            if (RequestAmount[resource] + change <= 0) return;
            RequestAmount[resource] += change;
        }
        public void ChangePurchasePrice(ResourceId resource, int change) {
            if (PurchasePrice[resource] + change <= 0) return;
            PurchasePrice[resource] += change;
        }
        public void ChangeDemand(ResourceId resource, int change) {
            ChangeRequestAmount(resource, change);
            ChangePurchasePrice(resource, change);
            Log.Msg("[ResourcePurchaser] {0} demand changed by {1} for actor {2}", resource, change, transform.name);
        }

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);
        }
    }
}