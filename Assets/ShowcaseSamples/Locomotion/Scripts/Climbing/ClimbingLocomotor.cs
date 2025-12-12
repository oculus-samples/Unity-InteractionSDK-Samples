/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Oculus.Interaction.Locomotion.ClimbingEvent;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// The ClimbingLocomotor listens for <see cref="ClimbingEvent"/> incoming from the
    /// LocomotionEvent pipeline and transformers them into actual Locomotion movement that
    /// can be processed by a simpler locomotor such as the <see cref="FirstPersonLocomotor"/>
    /// </summary>
    public class ClimbingLocomotor : MonoBehaviour,
        ILocomotionEventHandler, ILocomotionEventBroadcaster,
        IDeltaTimeConsumer
    {
        [SerializeField]
        [Tooltip("If true, only the last grab point will be used for moving the player. (recommended)")]
        private bool _lastGrabMoves = true;
        /// <summary>
        /// If true, only the last grab point will be used for moving the player.
        /// Otherwise, all grab points will be averaged.
        /// </summary>
        public bool LastGrabMoves
        {
            get => _lastGrabMoves;
            set => _lastGrabMoves = value;
        }

        [SerializeField]
        [Tooltip("If true, the player will slide when at least two hands are grabbing.")]
        private bool _twoHandsSlide = true;
        /// <summary>
        /// If true, the player will slide when at least two hands are grabbing.
        /// Otherwise, the player will move when at least one hand is grabbing.
        /// </summary>
        public bool TwoHandsSlide
        {
            get => _twoHandsSlide;
            set => _twoHandsSlide = value;
        }

        [SerializeField, Optional]
        [Tooltip("The character's feet transform. This is only needed for the Pull-up mechanism.")]
        private Transform _characterFeet;
        /// <summary>
        /// The character's feet transform. This is only needed for the Pull-up mechanism.
        /// </summary>
        public Transform CharacterFeet
        {
            get => _characterFeet;
            set => _characterFeet = value;
        }

        [SerializeField, Optional, ConditionalHide(nameof(_characterFeet), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("The speed of the transition to the top of the climbable when pulling up.")]
        private float _transitionSpeed = 2f;
        /// <summary>
        /// The speed of the transition to the top of the climbable when pulling up.
        /// </summary>
        public float TransitionSpeed
        {
            get => _transitionSpeed;
            set => _transitionSpeed = value;
        }

        [SerializeField, Optional, ConditionalHide(nameof(_characterFeet), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("The vertical distance to the pull-up target before starting the transition.")]
        private float _transitionStartThreshold = 0.5f;
        /// <summary>
        /// The vertical distance to the pull-up target before starting the transition.
        /// </summary>
        public float TransitionStartThreshold
        {
            get => _transitionStartThreshold;
            set => _transitionStartThreshold = value;
        }

        [SerializeField, Optional, ConditionalHide(nameof(_characterFeet), null, ConditionalHideAttribute.DisplayMode.HideIfTrue)]
        [Tooltip("Minimum distance to the pull-up target before stopping the transition.")]
        private float _transitionEndThreshold = 0.05f;
        /// <summary>
        /// Minimum distance to the pull-up target before stopping the transition.
        /// </summary>
        public float TransitionEndThreshold
        {
            get => _transitionEndThreshold;
            set => _transitionEndThreshold = value;
        }

        [SerializeField, Optional]
        [Tooltip("Context used for retrieving ClimbingEvents from the incoming LocomotionEvents.")]
        private Context _context;

        private Func<float> _deltaTimeProvider = () => Time.deltaTime;
        public void SetDeltaTimeProvider(Func<float> deltaTimeProvider)
        {
            _deltaTimeProvider = deltaTimeProvider;
        }

        /// <summary>
        /// Unique identifier of the locomotor.
        /// This iden
        /// </summary>
        public int Identifier => _identifier.ID;
        /// <summary>
        /// Whether the player is currently climbing.
        /// </summary>
        public bool IsClimbing => _climbingStates.Count > 0;
        /// <summary>
        /// Whether the player is currently sliding.
        /// </summary>
        public bool IsSliding => IsClimbing && _isSliding;
        /// <summary>
        /// Whether the player is currently pulling up towards a target.
        /// </summary>
        public bool IsPullingUp => IsClimbing && _isPullingUp;
        /// <summary>
        /// Indicates that the player has started climbing.
        /// </summary>
        public UnityEvent WhenClimbingStarted = new UnityEvent();
        /// <summary>
        /// Indicates that the player has stopped climbing
        /// and the velocity of the release.
        /// </summary>
        public UnityEvent<Vector3> WhenClimbingEnded = new UnityEvent<Vector3>();

        public Action<LocomotionEvent, Pose> _whenLocomotionEventHandled = delegate { };
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled
        {
            add => _whenLocomotionEventHandled += value;
            remove => _whenLocomotionEventHandled -= value;
        }

        public Action<LocomotionEvent> _whenLocomotionPerformed = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add => _whenLocomotionPerformed += value;
            remove => _whenLocomotionPerformed -= value;
        }

        private struct ClimbingState
        {
            public readonly int identifier;
            public readonly Climbable climbable;

            public Pose prevWorldPose;
            public Pose prevClimbablePose;
            public Vector3 translationDelta;

            public ClimbingState(int identifier, Climbable climbable)
            {
                this.identifier = identifier;
                this.climbable = climbable;
                translationDelta = Vector3.zero;
                prevWorldPose = Pose.identity;
                prevClimbablePose = climbable.Transform.GetPose();
            }

            public void ClearDelta()
            {
                translationDelta = Vector3.zero;
            }
        }

        private UniqueIdentifier _identifier;
        private List<ClimbingState> _climbingStates = new();
        private Vector3 _climbingVelocity = Vector3.zero;
        private bool _isSliding;
        private bool _isPullingUp;

        private static readonly float _velocityDampness = 100f;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(_context != null ? _context : Context.Global.GetInstance(), this);
        }

        protected virtual void LateUpdate()
        {
            if (IsClimbing)
            {
                Climb();
            }
            else
            {
                _isPullingUp = false;
                _isSliding = false;
            }
        }

        #region Locomotion Events Handling

        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None
                && locomotionEvent.Rotation == LocomotionEvent.RotationType.None)
            {
                if (TryGetLocomotionClimbingEvent(locomotionEvent,
                    out ClimbingEvent climbingEvent, _context))
                {
                    ProcessClimbingEvent(locomotionEvent, climbingEvent);
                    _whenLocomotionEventHandled.Invoke(locomotionEvent, Pose.identity);
                }
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.Relative)
            {
                CancelAllClimbables();
            }
        }

        private void ProcessClimbingEvent(LocomotionEvent locomotionEvent, ClimbingEvent climbingEvent)
        {
            if (climbingEvent.EventType == ClimbingEventType.Start)
            {
                ClimbingState climbingState = new ClimbingState(locomotionEvent.Identifier, climbingEvent.Climbable)
                {
                    prevWorldPose = locomotionEvent.Pose
                };

                _climbingStates.Add(climbingState);

                if (_climbingStates.Count == 1)
                {
                    StartClimbing();
                }
            }
            else if (climbingEvent.EventType == ClimbingEventType.End)
            {
                if (!IsValidID(locomotionEvent.Identifier, out int index))
                {
                    return;
                }

                _climbingStates.RemoveAt(index);

                if (_isSliding || !_lastGrabMoves)
                {
                    ClearDeltas();
                }

                if (_climbingStates.Count == 0)
                {
                    StopClimbing();
                }
            }
            else if (climbingEvent.EventType == ClimbingEventType.Move)
            {
                if (!IsValidID(locomotionEvent.Identifier, out int index))
                {
                    return;
                }

                ClimbingState climbingState = _climbingStates[index];

                Vector3 climbDelta = climbingState.prevWorldPose.position - locomotionEvent.Pose.position;
                Pose prevClimbablePose = climbingState.prevClimbablePose;
                Pose currentClimbablePose = climbingEvent.Climbable.Transform.GetPose();
                Vector3 objectDelta = currentClimbablePose.position - prevClimbablePose.position;
                climbDelta = objectDelta + climbDelta;

                if (!Mathf.Approximately(0f, climbingEvent.Climbable.SlideVelocity.sqrMagnitude))
                {
                    climbDelta = climbDelta - Vector3.Project(climbDelta, climbingEvent.Climbable.SlideVelocity.normalized);
                }

                climbingState.prevWorldPose = locomotionEvent.Pose;
                climbingState.prevClimbablePose = currentClimbablePose;
                climbingState.translationDelta += climbDelta;
                _climbingStates[index] = climbingState;
            }

            bool IsValidID(int identifier, out int index)
            {
                for (int i = 0; i < _climbingStates.Count; i++)
                {
                    if (_climbingStates[i].identifier == identifier)
                    {
                        index = i;
                        return true;
                    }
                }

                index = -1;
                return false;
            }
        }

        private void StartClimbing()
        {
            ClearState();
            WhenClimbingStarted.Invoke();
        }

        private void StopClimbing()
        {
            WhenClimbingEnded.Invoke(_climbingVelocity);
            ClearState();
        }

        private void ClearState()
        {
            _climbingVelocity = Vector3.zero;
            _isSliding = false;
            _isPullingUp = false;
        }

        private void ClearDeltas()
        {
            for (int i = 0; i < _climbingStates.Count; i++)
            {
                _climbingStates[i].ClearDelta();
            }
        }

        private void CancelAllClimbables()
        {
            while (_climbingStates.Count > 0)
            {
                ClimbingState climbingState = _climbingStates[0];
                climbingState.climbable.ProcessPointerEvent(new PointerEvent(
                        climbingState.identifier, PointerEventType.Cancel, climbingState.prevWorldPose));
            }

            ClearDeltas();
            ClearState();
        }

        private void Climb()
        {
            if (ShouldPullUp(out Transform transitionTarget))
            {
                if (!_isPullingUp)
                {
                    _isPullingUp = true;
                    _isSliding = false;
                }

                if (PullUp(transitionTarget))
                {
                    _isPullingUp = false;
                }
                return;
            }
            else
            {
                _isPullingUp = false;
            }

            Pose delta = Pose.identity;

            if (_lastGrabMoves)
            {
                delta.position = _climbingStates[_climbingStates.Count - 1].translationDelta;
            }
            else
            {
                Vector3 averageDelta = Vector3.zero;
                int count = _climbingStates.Count;
                for (int i = 0; i < count; i++)
                {
                    averageDelta += _climbingStates[i].translationDelta;
                }
                delta.position = averageDelta / count;
            }

            if (ShouldSlide(out Vector3 slideVelocity))
            {
                _isSliding = true;
                delta.position += slideVelocity * _deltaTimeProvider();
            }
            else
            {
                _isSliding = false;
            }

            _climbingVelocity = Vector3.Lerp(_climbingVelocity,
                delta.position / _deltaTimeProvider(),
                _deltaTimeProvider() * _velocityDampness);

            LocomotionEvent locomotionEvent = new LocomotionEvent(Identifier, delta,
                LocomotionEvent.TranslationType.Relative, LocomotionEvent.RotationType.Relative);
            _whenLocomotionPerformed.Invoke(locomotionEvent);
        }

        private bool ShouldSlide(out Vector3 averageSlide)
        {
            int sliders = 0;
            averageSlide = Vector3.zero;
            foreach (ClimbingState climbingState in _climbingStates)
            {
                Climbable climbable = climbingState.climbable;
                if (Mathf.Approximately(0f, climbable.SlideVelocity.sqrMagnitude))
                {
                    return false;
                }
                averageSlide += climbable.SlideVelocity;
                sliders++;
            }

            if (sliders == 0)
            {
                return false;
            }

            averageSlide /= sliders;
            return sliders >= (_twoHandsSlide ? 2 : 1);
        }

        private bool ShouldPullUp(out Transform transitionTarget)
        {
            transitionTarget = null;
            if (_characterFeet == null)
            {
                return false;
            }

            foreach (ClimbingState climbingState in _climbingStates)
            {
                Climbable climbable = climbingState.climbable;
                if (climbable.PullUpTarget == null
                    || (transitionTarget != null && climbable.PullUpTarget != transitionTarget))
                {
                    return false;
                }
                transitionTarget = climbable.PullUpTarget;
            }
            if (transitionTarget == null)
            {
                return false;
            }

            return _characterFeet.position.y >= transitionTarget.position.y - _transitionStartThreshold;
        }

        private bool PullUp(Transform target)
        {
            bool isCloseToTarget = Vector3.SqrMagnitude(target.position - _characterFeet.position)
                <= _transitionEndThreshold * _transitionEndThreshold;

            if (isCloseToTarget)
            {
                LocomotionEvent locomotionEvent = new LocomotionEvent(Identifier, target.position, LocomotionEvent.TranslationType.Absolute);
                _whenLocomotionPerformed.Invoke(locomotionEvent);
                CancelAllClimbables();
                return true;
            }
            else
            {
                Vector3 translationDelta = Vector3.MoveTowards(_characterFeet.position, target.position, _transitionSpeed * _deltaTimeProvider())
                    - _characterFeet.position;
                float magneticT = Mathf.Clamp01(Vector3.Dot(translationDelta.normalized, Vector3.up));
                translationDelta = Vector3.Slerp(translationDelta, Vector3.up * translationDelta.magnitude, magneticT);

                LocomotionEvent locomotionEvent = new LocomotionEvent(Identifier, translationDelta, LocomotionEvent.TranslationType.Relative);
                _whenLocomotionPerformed.Invoke(locomotionEvent);
            }
            return false;
        }
        #endregion

        #region Inject

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion
    }
}
