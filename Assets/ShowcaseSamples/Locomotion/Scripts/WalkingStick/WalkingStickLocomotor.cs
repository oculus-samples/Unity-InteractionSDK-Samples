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

using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Oculus.Interaction.Locomotion.LocomotionActionsBroadcaster;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// The WalkingStickLocomotor allows the user to move using one or several <see cref="WalkingStick"/>.
    /// It transform WalkingStick input into <see cref="LocomotionEvent"/> that can be used to move the player.
    /// When the sticks are pushed against the floor, the user will be translated in the direction of the sticks using Relative movements.
    /// When the sticks are released, the user will continue moving in the same direction for a short time using Velocity movements.
    /// If both sticks are pushed down with a strong force, the user will be able to perform a Jump.
    /// </summary>
    public class WalkingStickLocomotor : MonoBehaviour,
        ILocomotionEventBroadcaster, IDeltaTimeConsumer
    {
        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        [Tooltip("Transformer is required so calculations can be done in Tracking space")]
        private UnityEngine.Object _transformer;
        private ITrackingToWorldTransformer Transformer { get; set; }

        [SerializeField, Interface(typeof(IActiveState))]
        [Tooltip("Indicates if the character is touching the ground")]
        private UnityEngine.Object _isGrounded;
        private IActiveState IsGrounded { get; set; }

        [SerializeField]
        [Tooltip("Transform indicating the head of the character")]
        private Transform _head;

        [Header("Moving")]
        [SerializeField]
        [Tooltip("Factor to apply to the delta of the WalkingStick movement while pushing.")]
        private AnimationCurve _deltaFactor = AnimationCurve.Linear(0, 0, 1, 1);
        /// <summary>
        /// Factor to apply to the delta of the WalkingStick movement while pushing.
        /// </summary>
        public AnimationCurve DeltaFactor
        {
            get => _deltaFactor;
            set => _deltaFactor = value;
        }
        [SerializeField]
        [Tooltip("Factor to apply to the velocity when releasing the stick and starting a slide free.")]
        private AnimationCurve _velocityFactor = AnimationCurve.Linear(0, 0, 1, 1);
        /// <summary>
        /// Factor to apply to the velocity when releasing the stick and starting a slide free.
        /// </summary>
        public AnimationCurve VelocityFactor
        {
            get => _velocityFactor;
            set => _velocityFactor = value;
        }
        [SerializeField]
        [Tooltip("Stickiness of the direction of the WalkingStick movement towards the current direction of the character.")]
        private AnimationCurve _aimingStickiness = AnimationCurve.Constant(-1, 1, 0);
        /// <summary>
        /// Stickiness of the direction of the WalkingStick movement towards the current direction of the character.
        /// </summary>
        public AnimationCurve AimingStickiness
        {
            get => _aimingStickiness;
            set => _aimingStickiness = value;
        }
        [SerializeField]
        [Tooltip("Strength of the movement when moving in the direction of the character.")]
        private AnimationCurve _directionStrength = AnimationCurve.Constant(-1, 1, 1);
        /// <summary>
        /// Strength of the movement when moving in the direction of the character.
        /// </summary>
        public AnimationCurve DirectionStrength
        {
            get => _directionStrength;
            set => _directionStrength = value;
        }

        [Header("Jumping")]
        [SerializeField]
        [Tooltip("Bias towards forward direction when jumping.")]
        private AnimationCurve _jumpForwardFactor = AnimationCurve.Constant(-1, 1, 1);
        /// <summary>
        /// Bias towards forward direction when jumping.
        /// </summary>
        public AnimationCurve JumpForwardFactor
        {
            get => _jumpForwardFactor;
            set => _jumpForwardFactor = value;
        }
        [SerializeField]
        [Tooltip("Factor to apply to the jump horizontal velocity.")]
        private AnimationCurve _jumpVelocityFactor = AnimationCurve.Constant(-1, 1, 1);
        /// <summary>
        /// Factor to apply to the jump horizontal velocity.
        /// </summary>
        public AnimationCurve JumpVelocityFactor
        {
            get => _jumpVelocityFactor;
            set => _jumpVelocityFactor = value;
        }
        [SerializeField]
        [Tooltip("Factor to apply to the jump vertical velocity.")]
        private AnimationCurve _jumpVerticalFactor = AnimationCurve.Constant(-1, 1, 1);
        /// <summary>
        /// Factor to apply to the jump vertical velocity.
        /// </summary>
        public AnimationCurve JumpVerticalFactor
        {
            get => _jumpVerticalFactor;
            set => _jumpVerticalFactor = value;
        }

        [SerializeField]
        private UnityEvent _whenStarted = new();
        /// <summary>
        /// Raised when the user is using any WalkingStick
        /// </summary>
        public UnityEvent WhenStarted => _whenStarted;

        [SerializeField]
        private UnityEvent _whenEnded = new();
        /// <summary>
        /// Raised when the user is not using any WalkingStick
        /// </summary>
        public UnityEvent WhenEnded => _whenEnded;

        private float _stickLength;
        /// <summary>
        /// The length of the stick, in world units.
        /// </summary>
        public float StickLength => _stickLength;

        private UniqueIdentifier _identifier;
        /// <summary>
        /// Unique identifier of this Locomotion Handler
        /// </summary>
        public int Identifier => _identifier.ID;

        private Func<float> _deltaTimeProvider = () => Time.deltaTime;
        public void SetDeltaTimeProvider(Func<float> deltaTimeProvider)
        {
            _deltaTimeProvider = deltaTimeProvider;
        }

        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };

        private struct WalkingStickState
        {
            public Vector3 velocity;
            public Vector3 point;
            public Vector3 delta;
            public bool stuck;

            private static readonly int _highConfidenceFrames = 5;
            private int _highConfidenceCount;
            private bool _isHighConfidence;
            public bool IsHighConfidence
            {
                get
                {
                    return _isHighConfidence;
                }
                set
                {
                    if (value
                        && ++_highConfidenceCount >= _highConfidenceFrames)
                    {
                        _isHighConfidence = true;
                    }
                    else if (!value)
                    {
                        _highConfidenceCount = 0;
                        _isHighConfidence = false;
                    }
                }
            }

            private bool _isTouchingFloor;
            public bool IsTouchingFloor
            {
                get => _isHighConfidence && _isTouchingFloor;
                set => _isTouchingFloor = value;
            }
        }

        private List<WalkingStick> _sticks = new List<WalkingStick>();
        private List<WalkingStickState> _states = new List<WalkingStickState>();

        protected bool _started;

        private Vector3 _pushingVelocity;
        private Vector3 _globalVelocity;
        private float _floorHeight;
        private float _floorVelocity;
        private int _prevTouchingSticksCount;
        private float _jumpVelocityThreshold = _jumpVelocityThresholdMax;
        private float _lastJumpTime;

        private static readonly float _stickSkin = 0.08f;
        private static readonly float _gravity = 2.0f;
        private static readonly float _pushThreshold = 0.02f;
        private static readonly float _pushFloorFactor = 1.1f;
        private static readonly float _lengthFactor = 0.68f;
        private static readonly float _velocityDampness = 100f;
        private static readonly float _slideEpsilon = 0.001f;
        private static readonly float _jumpSameHeight = 0.08f;
        private static readonly float _jumpHeadDistance = 0.1f;
        private static readonly float _jumpPushingUpThreshold = 0.01f;
        private static readonly float _jumpVelocityThresholdMin = 2f;
        private static readonly float _jumpVelocityThresholdMax = 3f;
        private static readonly float _jumpThresholdVelocity = 5f;
        private static readonly float _jumpRelaxVelocity = 1f;
        private static readonly float _jumpTimeThreshold = 0.5f;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);

            if (Transformer == null)
            {
                Transformer = _transformer as ITrackingToWorldTransformer;
            }

            if (IsGrounded == null)
            {
                IsGrounded = _isGrounded as IActiveState;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(Transformer, nameof(_transformer));
            this.AssertField(_head, nameof(_head));
            this.AssertField(IsGrounded, nameof(_isGrounded));

            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            for (int i = 0; i < _sticks.Count; i++)
            {
                ProcessStick(_sticks[i], i);
            }

            ProcessMovement();
        }

        internal void RegisterStick(WalkingStick stick)
        {
            _sticks.Add(stick);
            _states.Add(default);
            if (_sticks.Count == 1)
            {
                _stickLength = CalculateStickLength();
                _whenStarted.Invoke();
            }
        }

        internal void UnregisterStick(WalkingStick stick)
        {
            int index = _sticks.IndexOf(stick);
            _sticks.RemoveAt(index);
            _states.RemoveAt(index);

            if (_sticks.Count == 0)
            {
                _stickLength = 0f;
                _whenEnded.Invoke();
            }
        }

        private void ProcessMovement()
        {
            float minHeight = GetPushingWalkingStick(out int index);
            float verticalDelta = index >= 0 ? _states[index].delta.y : 0f;
            PushFloor(verticalDelta, minHeight);

            Vector3 instantVelocity = Vector3.zero;
            int pushingDownSticksCount = 0;
            for (int i = 0; i < _sticks.Count; i++)
            {
                WalkingStickState state = _states[i];
                _sticks[i].Pushing = state.IsTouchingFloor;
                if (state.IsTouchingFloor)
                {
                    float verticalVelocity = state.delta.y / _deltaTimeProvider();
                    if (verticalVelocity <= _pushThreshold)
                    {
                        continue;
                    }

                    pushingDownSticksCount++;
                    instantVelocity += new Vector3(state.delta.x, 0f, state.delta.z);
                }

                if (!state.stuck && state.IsTouchingFloor)
                {
                    _sticks[i].Stick();
                    state.stuck = true;
                }
                else if (state.stuck && _states[i].point.y >= _stickSkin)
                {
                    _sticks[i].Unstick();
                    state.stuck = false;
                }

                _states[i] = state;
            }

            if (pushingDownSticksCount > 0)
            {
                instantVelocity /= pushingDownSticksCount;
                _pushingVelocity = Vector3.Lerp(_pushingVelocity, instantVelocity,
                    _deltaTimeProvider() * _velocityDampness);
            }

            UpdateGlobalVelocity();

            bool jump = ProcessJump(out float jumpStrength);
            if (jump && IsGrounded.Active)
            {
                Vector3 jumpVelocity = _globalVelocity;
                RetargetJump(ref jumpVelocity);
                Slide(jumpVelocity);
                jumpStrength = _jumpVerticalFactor.Evaluate(jumpStrength);
                Jump(jumpStrength);
                CancelAll();
                pushingDownSticksCount = 0;
            }
            else if (pushingDownSticksCount > 0
                && IsGrounded.Active)
            {
                Push(instantVelocity);
            }
            else if (_prevTouchingSticksCount > 0)
            {
                Slide(_pushingVelocity / _deltaTimeProvider());
            }

            if (pushingDownSticksCount == 0)
            {
                _pushingVelocity = Vector3.zero;
            }

            _prevTouchingSticksCount = pushingDownSticksCount;
        }

        private void UpdateGlobalVelocity()
        {
            Vector3 globalVelocity = Vector3.zero;
            for (int i = 0; i < _sticks.Count; i++)
            {
                Vector3 d = _states[i].delta;
                globalVelocity += d / _sticks.Count;
            }
            _globalVelocity = Vector3.Lerp(_globalVelocity, globalVelocity,
                _deltaTimeProvider() * _velocityDampness);
        }

        private void CancelAll(int skipID = -1)
        {
            _globalVelocity = Vector3.zero;
            _pushingVelocity = Vector3.zero;
            for (int i = 0; i < _states.Count; i++)
            {
                if (i != skipID)
                {
                    var state = _states[i];
                    state.IsHighConfidence = false;
                    state.stuck = false;
                    _states[i] = state;
                }
            }
        }

        private float CalculateStickLength()
        {
            Pose pose = Transformer.ToTrackingPose(_head.GetPose());
            float h = pose.position.y * _lengthFactor;
            return Mathf.Max(0f, h);
        }

        private void ProcessStick(WalkingStick stick, int index)
        {
            WalkingStickState state = _states[index];
            Vector3 prevPoint = state.point;
            state.point = GetWalkingStickPoint(stick);
            state.delta = CalculateDelta(prevPoint, state.point);
            state.IsTouchingFloor = IsTouchingFloor(state.point, _stickSkin);

            if (!stick.IsHighConfidence())
            {
                state.IsHighConfidence = false;
                state.delta = Vector3.zero;
            }
            else if (!state.IsHighConfidence)
            {
                state.IsHighConfidence = true;
                state.delta = Vector3.zero;
            }

            Vector3 instantVelocity = Vector3.ProjectOnPlane(state.delta, Vector3.up);
            float velocityDot = Vector3.Dot(state.velocity, instantVelocity);
            if (velocityDot < -0.5f)
            {
                instantVelocity = Vector3.zero;
                state.delta.x = state.delta.z = 0;
            }

            state.velocity = Vector3.Lerp(state.velocity, instantVelocity, _deltaTimeProvider());

            _states[index] = state;
        }

        private Vector3 GetWalkingStickPoint(WalkingStick stick)
        {
            Pose trackingPose = Transformer.ToTrackingPose(stick.transform.GetPose());
            return trackingPose.position + Vector3.down * _stickLength;
        }

        private Vector3 CalculateDelta(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta = -Transformer.Transform.TransformVector(delta);
            float verticalDelta = delta.y;
            delta.y = 0;
            AdjustSpeed(ref delta, _deltaFactor);
            delta.y = verticalDelta;
            return delta;
        }

        private bool IsTouchingFloor(Vector3 point, float skin)
        {
            return point.y <= skin + _floorHeight;
        }

        private float GetPushingWalkingStick(out int index)
        {
            float minHeight = 0f;
            index = -1;
            for (int i = 0; i < _states.Count; i++)
            {
                if (!_states[i].IsHighConfidence)
                {
                    continue;
                }

                float height = _states[i].point.y;
                if (height < minHeight)
                {
                    index = i;
                    minHeight = height;
                }
            }
            return minHeight;
        }

        private void AdjustSpeed(ref Vector3 delta, AnimationCurve curve)
        {
            float speed = delta.magnitude;
            speed = curve.Evaluate(speed);
            delta = delta.normalized * speed;
        }

        private void Retarget(ref Vector3 delta)
        {
            Vector3 aimingForward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            float dot = Vector3.Dot(delta.normalized, aimingForward);
            float t = _aimingStickiness.Evaluate(dot);
            if (t < 0)
            {
                aimingForward = -aimingForward;
            }
            delta = Vector3.Slerp(delta, aimingForward * delta.magnitude, t);
            delta *= _directionStrength.Evaluate(dot);
        }

        private void RetargetJump(ref Vector3 delta)
        {
            delta.y = 0;
            delta = delta / _deltaTimeProvider();
            float lateralJump = Mathf.Abs(Vector3.Dot(delta,
                Vector3.ProjectOnPlane(_head.right, Vector3.up).normalized));

            delta += Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized * _jumpForwardFactor.Evaluate(lateralJump);
            AdjustSpeed(ref delta, _jumpVelocityFactor);
        }

        private bool ProcessJump(out float strength)
        {
            if (_states.Count != 2
                || Time.time - _lastJumpTime < _jumpTimeThreshold)
            {
                strength = 0f;
                _jumpVelocityThreshold = _jumpVelocityThresholdMax;
                return false;
            }

            if (AreParallel())
            {
                float verticalVelocity = VerticalVelocity();
                if (verticalVelocity < 0.1f)
                {
                    _jumpVelocityThreshold = Mathf.MoveTowards(_jumpVelocityThreshold,
                        _jumpVelocityThresholdMin,
                        _deltaTimeProvider() * -verticalVelocity * _jumpThresholdVelocity);
                }
                else if (verticalVelocity > _jumpVelocityThreshold
                    && Mathf.Max(_sticks[0].transform.position.y,
                        _sticks[1].transform.position.y)
                    < _head.transform.position.y - _jumpHeadDistance)
                {
                    strength = verticalVelocity;

                    _jumpVelocityThreshold = _jumpVelocityThresholdMax;
                    _lastJumpTime = Time.time;
                    return true;
                }
                else
                {
                    _jumpVelocityThreshold = Mathf.MoveTowards(_jumpVelocityThreshold,
                        _jumpVelocityThresholdMax,
                        _deltaTimeProvider() * _jumpRelaxVelocity);
                }
            }
            else
            {
                _jumpVelocityThreshold = Mathf.MoveTowards(_jumpVelocityThreshold,
                    _jumpVelocityThresholdMax,
                    _deltaTimeProvider() * _jumpThresholdVelocity);
            }

            strength = 0f;
            return false;

            bool AreParallel()
            {
                float pA = _states[0].point.y;
                float pB = _states[1].point.y;
                bool sameHeight = Mathf.Abs(pA - pB) <= _jumpSameHeight;

                float vA = _states[0].delta.y;
                float vB = _states[1].delta.y;
                bool sameVelocity = (Mathf.Abs(vA) <= _jumpPushingUpThreshold
                    && Mathf.Abs(vB) <= _jumpPushingUpThreshold)
                    || Mathf.Abs(vA - vB) <= _jumpPushingUpThreshold;

                return sameHeight && sameVelocity;
            }

            float VerticalVelocity()
            {
                float a = _states[0].delta.y;
                float b = _states[1].delta.y;

                float max = Mathf.Max(a, b);
                return max / _deltaTimeProvider();
            }
        }

        private void PushFloor(float verticalDelta, float height)
        {
            if (verticalDelta >= 0 || height > _floorHeight)
            {
                verticalDelta = 0;
            }

            _floorVelocity += verticalDelta * _pushFloorFactor * _deltaTimeProvider();
            _floorVelocity += _gravity * _deltaTimeProvider();
            _floorHeight += _floorVelocity * _deltaTimeProvider();

            if (_floorHeight >= height)
            {
                _floorHeight = height;
                _floorVelocity = 0;
            }
        }

        private void Slide(Vector3 delta)
        {
            AdjustSpeed(ref delta, _velocityFactor);
            if (delta.sqrMagnitude > _slideEpsilon)
            {
                Retarget(ref delta);

                LocomotionEvent locomotionEvent = new LocomotionEvent(Identifier,
                    new Pose(delta, Quaternion.LookRotation(delta.normalized)),
                    LocomotionEvent.TranslationType.Velocity, LocomotionEvent.RotationType.None);
                WhenLocomotionPerformed.Invoke(locomotionEvent);
            }
        }

        private void Push(Vector3 delta)
        {
            Retarget(ref delta);
            LocomotionEvent locomotionEvent = new LocomotionEvent(Identifier, delta,
                LocomotionEvent.TranslationType.Relative);
            WhenLocomotionPerformed.Invoke(locomotionEvent);
        }

        private void Jump(float force)
        {
            Pose pose = new Pose(Vector3.up * force, Quaternion.identity);
            LocomotionEvent locomotionEvent = CreateLocomotionEventAction(this.Identifier,
                LocomotionAction.Jump, pose);
            this.WhenLocomotionPerformed.Invoke(locomotionEvent);
            DisposeLocomotionAction(locomotionEvent);
        }

        #region  Inject

        public void InjectAllWalkingStickLocomotor(
            ITrackingToWorldTransformer transformer,
            IActiveState isGrounded,
            Transform head)
        {
            InjectTrackingToWorldTransformer(transformer);
            InjectIsGrounded(isGrounded);
            InjectHead(head);
        }

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer transformer)
        {
            _transformer = transformer as UnityEngine.Object;
            Transformer = transformer;
        }

        public void InjectIsGrounded(IActiveState isGrounded)
        {
            _isGrounded = isGrounded as UnityEngine.Object;
            IsGrounded = isGrounded;
        }

        public void InjectHead(Transform head)
        {
            _head = head;
        }

        #endregion

    }
}
