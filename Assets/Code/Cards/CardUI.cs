using BeauRoutine;
using EasyAssetStreaming;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zavala.Cards {
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public int PolicyIndex; // Which severity index this card corresponds to (also index from left to right)
        public Button Button;
        public TMP_Text Text;
        public Image CardArt;
        public Graphic Background;



        private void OnDisable() {
            Button.onClick.RemoveAllListeners();
        }
        public void OnPointerEnter(PointerEventData eventData) {
            transform.SetAsLastSibling();
            transform.SetScale(1.1f);
        }
        public void OnPointerExit(PointerEventData eventData) {
            transform.SetScale(1);
        }
    }

    static public class CardUIUtility { 
        static public void PopulateCard(CardUI card, CardData data, Color32 bgColor) {
            ExtractLocText(data, out string locText);
            // TODO: extract font effects
            card.Text.SetText(locText);
            ExtractSprite(data, out Sprite sprite);
            card.CardArt.sprite = sprite;
            card.PolicyIndex = (int)data.PolicyLevel;
            card.Background.color = bgColor;
        }

        static public void ExtractLocText(CardData data, out string locText) {
            string typeText = Loc.Find("cards." + data.PolicyType.ToString() + ".category");
            string severityText;
            if (data.PolicyLevel == PolicyLevel.Alt) {
                // Varies across policy types
                severityText = Loc.Find("cards." + data.PolicyType.ToString() + "." + data.PolicyLevel.ToString().ToLower());
            }
            else {
                // Same across policy types
                severityText = Loc.Find("cards.severity." + data.PolicyLevel.ToString().ToLower());
            }

            locText = typeText + ": " + severityText.ToUpper();
        }

        static public void ExtractSprite(CardData data, out Sprite sprite) {
            // find image path from card definition, load it from resources
            string pathStr = data.ImgPath;
            int extIndex = pathStr.IndexOf(".");
            if (extIndex != -1) {
                pathStr = pathStr.Substring(0, extIndex);
            }
            sprite = Resources.Load<Sprite>("CardArt/" + pathStr);
        }
    }

}
