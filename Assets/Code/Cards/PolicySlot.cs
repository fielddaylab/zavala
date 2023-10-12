using BeauRoutine;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Sim;

namespace Zavala.Cards
{
    public class PolicySlot : MonoBehaviour
    {
        [SerializeField] private PolicyType m_Type;
        [SerializeField] private Button m_Button;
        [SerializeField] private TMP_Text m_Text;

        [SerializeField] private Image m_OverlayImage; // The locked/open slot image
        [SerializeField] private Graphic m_SlotBackground;
        [SerializeField] private Sprite m_LockedSprite;
        [SerializeField] private Sprite m_UnlockedSprite;

        private Color32 m_LockedColor;
        private Color32 m_UnlockedColor;
        private Color32 m_SlotColor;

        private Routine m_ChoiceRoutine;
        private List<CardUI> m_DisplayCards;

        private enum HandState {
            Hidden,
            Showing,
            Visible,
            Hiding
        }

        private HandState m_HandState;

        // TODO: collapse menus when background clicked
        // TODO: propagate menu collapse through children

        private void Start() {
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            m_Button.onClick.AddListener(() => { policyState.PolicySlotClicked?.Invoke(m_Type); });
            policyState.PolicySlotClicked.Register(HandleSlotClicked);
            policyState.PolicyCardSelected.Register(HandlePolicyCardSelected);
            policyState.PolicyCloseButtonClicked.Register(HandlePolicyCloseClicked);

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked.Register(HandleAdvisorButtonClicked);

            m_HandState = HandState.Hidden;
        }

        public void PopulateSlot(PolicyType newType) {
            m_Type = newType;

            // If this slot type is unlocked, enable button / disable if not
            CardsState state = Game.SharedState.Get<CardsState>();
            List<CardData> unlockedCards = CardsUtility.GetUnlockedOptions(state, m_Type);
            bool slotUnlocked = unlockedCards.Count > 0;
            if (slotUnlocked) {
                m_OverlayImage.enabled = true; // set to open slot image
                m_OverlayImage.sprite = m_UnlockedSprite;
                m_OverlayImage.color = m_UnlockedColor;
                m_SlotBackground.color = m_SlotColor;
                m_Button.enabled = true;
            }
            else {
                m_OverlayImage.enabled = true; // set to locked slot image
                m_OverlayImage.sprite = m_LockedSprite;
                m_OverlayImage.color = m_LockedColor;
                m_SlotBackground.color = m_UnlockedColor; // same color is used for locked background as locked foreground
                m_Button.enabled = false;
            }
            m_Text.SetText("");

            // TODO: if we add possibility for no policy to be selected, implement check here

            if (slotUnlocked) {
                bool currentExists = false;
                // load current policy
                PolicyState policyState = Game.SharedState.Get<PolicyState>();
                SimGridState grid = Game.SharedState.Get<SimGridState>();
                PolicyLevel level = policyState.Policies[grid.CurrRegionIndex].Map[newType];
                if (policyState.Policies[grid.CurrRegionIndex].EverSet[newType]) {
                    // Has been set before -- look for current policy
                    for (int i = 0; i < unlockedCards.Count; i++) {
                        if (unlockedCards[i].PolicyLevel == level) {
                            // found current policy
                            MirrorSelectedCard(unlockedCards[i]);
                            currentExists = true;
                            break;
                        }
                    }
                }

                if (currentExists) {
                    // the overlay image acts as both the lock/unlock and the policy image, no need to change it here
                    //m_OverlayImage.enabled = false;
                }
            }
        }

        public void SetColors(Color slotColor, Color lockedColor, Color unlockedColor) {
            m_SlotColor = slotColor;
            m_LockedColor = lockedColor;
            m_UnlockedColor = unlockedColor;
        }

        #region Handlers

        private void HandleSlotClicked(PolicyType policyType) {
            if (policyType != m_Type) {
                // Clicked a different slot
                if (m_HandState == HandState.Visible || m_HandState == HandState.Showing) {
                    // Hide this hand in deference to other hand
                    m_ChoiceRoutine.Replace(HideHandRoutine());
                }
                return;
            }

            if (m_HandState == HandState.Hidden || m_HandState == HandState.Hiding) {
                // Show hand
                CardsState cardState = Game.SharedState.Get<CardsState>();
                CardPools pools = Game.SharedState.Get<CardPools>();
                PolicyState policyState = Game.SharedState.Get<PolicyState>();

                List<CardData> cardData = CardsUtility.GetUnlockedOptions(cardState, m_Type);
                if (m_DisplayCards == null) {
                    m_DisplayCards = new List<CardUI>();
                }
                else {
                    m_DisplayCards.Clear();
                }
                // For each card, allocate a card from the pool
                for (int i = 0; i < cardData.Count; i++) {
                    CardData data = cardData[i];
                    CardUI card = pools.Cards.Alloc(this.transform.parent != null ? this.transform.parent : this.transform);
                    CardUIUtility.PopulateCard(card, data, cardState.Sprites, m_SlotColor);

                    card.transform.localPosition = this.transform.localPosition;
                    card.transform.SetAsFirstSibling();
                    m_DisplayCards.Add(card);
                    card.Button.onClick.AddListener(() => { OnCardClicked(policyState, data, card); });
                }

                m_ChoiceRoutine.Replace(ShowHandRoutine());
            }
            else if (m_HandState == HandState.Visible || m_HandState == HandState.Showing) {
                // Hide hand
                m_ChoiceRoutine.Replace(HideHandRoutine());
            }
        }

        private void HandlePolicyCardSelected(CardData data) {
            if (m_Type == data.PolicyType) {
                MirrorSelectedCard(data);
            }

            // Hide Hand
            m_ChoiceRoutine.Replace(HideHandRoutine());
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) {
            if (gameObject.activeInHierarchy) {
                // Hide Hand
                m_ChoiceRoutine.Replace(HideHandRoutine());
            }
        }

        private void HandlePolicyCloseClicked() {
            // Hide Hand
            m_ChoiceRoutine.Replace(HideHandRoutine());
        }

        private void OnCardClicked(PolicyState policyState, CardData data, CardUI card) {
            policyState.PolicyCardSelected?.Invoke(data);
            // TODO: hiding/click into place animation
        }

        #endregion // Handlers

        private void MirrorSelectedCard(CardData data) {
            // Set this image and text to selected card's text and image
            CardUIUtility.ExtractSprite(data, Game.SharedState.Get<CardsState>().Sprites, out Sprite sprite);
            CardUIUtility.ExtractLocText(data, out string locText);
            // TODO: extract font effects
            m_OverlayImage.sprite = sprite;
            m_OverlayImage.color = Color.white;
            m_Text.SetText(locText);
            m_OverlayImage.enabled = true;
        }

        #region Routines

        private IEnumerator ShowHandRoutine() {
            m_HandState = HandState.Showing;

            float offset = 0.5f;
            float leftMost = 60 * (m_DisplayCards.Count - 1);
            float rotatedMost = 8.0f * (m_DisplayCards.Count - 1);
            float topMost = 155;
            for (int i = 0; i < m_DisplayCards.Count; i++) {
                RectTransform cardTransform = (RectTransform) m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                        cardTransform.AnchorPosTo(new Vector2(
                            cardTransform.anchoredPosition.x - leftMost + 120 * i,
                            cardTransform.anchoredPosition.y + topMost - 30 * Mathf.Abs((i + offset) - (m_DisplayCards.Count / 2.0f))
                        ), .3f, Axis.XY),
                        cardTransform.RotateTo(cardTransform.rotation.z + rotatedMost - 15f * i, .3f, Axis.Z)
                    )
                ).OnComplete(() => { m_HandState = HandState.Visible; });
            }

            yield return null;
        }


        private IEnumerator HideHandRoutine() {
            if (m_HandState == HandState.Hidden) {
                yield break;
            }

            m_HandState = HandState.Hiding;

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                Transform cardTransform = m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                    cardTransform.MoveTo(this.transform.position, .3f, Axis.XY),
                    cardTransform.RotateTo(0, .3f, Axis.Z)
                    )
                ).OnComplete(() => { m_HandState = HandState.Hidden; });
            }

            while (m_HandState != HandState.Hidden) {
                yield return null;
            }

            CardPools pools = Game.SharedState.Get<CardPools>();

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                // free the card back to the pool
                pools.Cards.Free(m_DisplayCards[i]);
            }
            m_DisplayCards.Clear();
        }

        #endregion // Routines
    }
}