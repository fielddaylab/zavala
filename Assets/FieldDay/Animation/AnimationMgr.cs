using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Animation {
    public class AnimationMgr {
        #region Types

        private struct LiteAnimatorRecord {
            public ILiteAnimator Animator;
            public LiteAnimatorState State;
        }

        #endregion // Types

        #region State

        private readonly RingBuffer<LiteAnimatorRecord> m_UpdateLiteAnimators = new RingBuffer<LiteAnimatorRecord>(16, RingBufferMode.Expand);
        private readonly RingBuffer<LiteAnimatorRecord> m_UnscaledUpdateLiteAnimators = new RingBuffer<LiteAnimatorRecord>(16, RingBufferMode.Expand);

        #endregion // State

        internal AnimationMgr() {

        }

        #region Lite Animators

        public void AddLiteAnimator(ILiteAnimator animator, float duration, GameLoopPhase phase = GameLoopPhase.Update) {
            AddLiteAnimator(animator, new LiteAnimatorState() {
                Duration = duration,
                TimeRemaining = duration
            }, phase);
        }

        public void AddLiteAnimator(ILiteAnimator animator, LiteAnimatorState state, GameLoopPhase phase = GameLoopPhase.Update) {
            Assert.NotNull(animator);
            var liteAnimators = GetLiteAnimators(phase);

            for (int i = 0; i < liteAnimators.Count; i++) {
                if (liteAnimators[i].Animator == animator) {
                    liteAnimators[i].State = state;
                    break;
                }
            }

            liteAnimators.PushBack(new LiteAnimatorRecord() {
                Animator = animator,
                State = state
            });
        }

        /// <summary>
        /// Cancels an animation.
        /// </summary>
        public void CancelLiteAnimator(ILiteAnimator animator, GameLoopPhase phase = GameLoopPhase.Update) {
            Assert.NotNull(animator);
            var liteAnimators = GetLiteAnimators(phase);
            for(int i = 0; i < liteAnimators.Count; i++) {
                if (liteAnimators[i].Animator == animator) {
                    liteAnimators.FastRemoveAt(i);
                    break;
                }
            }
        }

        private RingBuffer<LiteAnimatorRecord> GetLiteAnimators(GameLoopPhase phase) {
            switch (phase) {
                case GameLoopPhase.Update:
                    return m_UpdateLiteAnimators;
                case GameLoopPhase.UnscaledUpdate:
                    return m_UnscaledUpdateLiteAnimators;
                default:
                    Assert.Fail("LiteAnimators can only be updated on Update or UnscaledUpdate");
                    return null;
            }
        }

        #endregion // Lite Animators

        #region Events

        internal void Initialize() {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLite(float deltaTime) {
            HandleLateUpdate(GameLoopPhase.Update, deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UnscaledUpdateLite(float deltaTime) {
            HandleLateUpdate(GameLoopPhase.UnscaledUpdate, deltaTime);
        }

        private void HandleLateUpdate(GameLoopPhase phase, float deltaTime) {
            var liteAnimators = GetLiteAnimators(phase);
            int count = liteAnimators.Count;
            while (count-- > 0) {
                LiteAnimatorRecord animRecord = liteAnimators.PopFront();
                if (animRecord.Animator.UpdateAnimation(ref animRecord.State, deltaTime)) {
                    liteAnimators.PushBack(animRecord);
                }
            }
        }

        internal void Shutdown() {
            m_UpdateLiteAnimators.Clear();
            m_UnscaledUpdateLiteAnimators.Clear();
        }

        #endregion // Events
    }
}