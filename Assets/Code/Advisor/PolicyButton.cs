using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.World;

namespace Zavala.Advisor {
    public class PolicyButton : MonoBehaviour {
        public AdvisorType PolicyGroup;
        public PolicyType ButtonPolicy;
        public GameObject Cards;
        public TextMeshProUGUI Text;

        // TODO: collapse menus when background clicked
        // TODO: propagate menu collapse through children

        private void Start() {
            SetPolicy(0);    
            Cards.SetActive(false);
        }

        public void TogglePolicies(bool toggle) {
            Cards.SetActive(toggle);
            Log.Msg("[PolicyButton] Pressed, {0} active: {1}", Cards.transform.name, toggle);
            
        }

        public void SetPolicy(int policyIndex) {
            // try to set a policy: if successful, close the policies menu
            if (Game.SharedState.Get<PolicyState>().SetPolicy(ButtonPolicy, policyIndex)) {
                Text.text = ButtonPolicy.ToString() + ": " + policyIndex;
                TryGetComponent(out Toggle toggle);
                toggle.isOn = false;
                TogglePolicies(false);
            }
        }


    }

    public enum PolicyType : byte {
        RunoffPenalty,
        Skimming,
        ExportTax,
        SalesTax
    }


}