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

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TabletopGrabInertia : MonoBehaviour
    {
        [SerializeField]
        private Grabbable _grabbable;

        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _active;

        public IActiveState Active { get; private set; }
        [Header("Position Inertia")]
        [SerializeField]
        private bool _posX = true;
        [SerializeField]
        private bool _posY = false;

        [SerializeField]
        private bool _posZ = true;

        [SerializeField, Min(0)]
        private float _linearDrag = 5f;

        [SerializeField]
        private float _maxLinear = 25f;

        [SerializeField, Min(0)]
        [Tooltip("Maximum local distance from the origin, matching ConstrainedTransformer. Scaled by localScale.x squared.")]
        private float _maxDistance = 18f;

        [Header("Rotation Inertia")]
        [SerializeField, Optional]
        private Transform _rotPivot;
        [SerializeField]
        private bool _rotX = false;

        [SerializeField]
        private bool _rotY = true;

        [SerializeField]
        private bool _rotZ = false;

        [SerializeField, Min(0)]
        private float _angularDrag = 5f;

        [SerializeField]
        [Tooltip("Degrees Per Second")]
        private float _maxAngular = 360f;

        [Header("Velocity Tracking")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Smoothing applied to the tracked release velocity. 1 = raw per-frame velocity, lower = smoother but laggier.")]
        private float _velocitySmoothing = 0.5f;

        [SerializeField]
        [Tooltip("Suppress release velocity while the object is being scaled, so a two-hand scale gesture doesn't impart a throw.")]
        private bool _suppressVelocityWhileScaling = true;

        [SerializeField, Min(0)]
        [Tooltip("Fractional scale change per second at which release velocity is fully suppressed (e.g. 1 = 100%/s).")]
        private float _scaleSuppressionRate = 1f;

        private Vector3 _localLinear, _localAngular;
        private bool _applyInertia;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private Vector3 _prevScale;
        private float _prevTime;
        private bool _hasPrevSample;
        private int _activeSelectors;

        private void Awake()
        {
            Active = _active as IActiveState;
        }

        private void OnEnable()
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }

        private void OnDisable()
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                if (_activeSelectors == 0)
                {
                    _applyInertia = false;
                    _localLinear = Vector3.zero;
                    _localAngular = Vector3.zero;
                    _hasPrevSample = false;
                    _prevTime = -1f;
                }

                _activeSelectors++;
            }
            else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
            {
                _activeSelectors--;
            }

            if (evt.Type == PointerEventType.Unselect && _activeSelectors <= 0)
            {
                _activeSelectors = 0;
                ApplyAxisConstraints();
                _applyInertia = true;
            }
        }

        private void LateUpdate()
        {
            if (_applyInertia || _activeSelectors <= 0) return;
            if (Active is not { Active: true }) return;

            var time = Time.time;
            var pose = _grabbable.Transform.GetPose(Space.Self);
            var scale = _grabbable.Transform.localScale;

            if (_hasPrevSample)
            {
                var dt = time - _prevTime;
                if (dt > 0f)
                {
                    var linear = (pose.position - _prevPosition) / dt;

                    var deltaRot = pose.rotation * Quaternion.Inverse(_prevRotation);
                    deltaRot.ToAngleAxis(out var angle, out var axis);
                    if (angle > 180f) angle -= 360f;
                    var angular = axis.normalized * (angle / dt);

                    var suppression = ScaleSuppression(_prevScale, scale, dt);
                    linear *= suppression;
                    angular *= suppression;

                    _localLinear = Vector3.Lerp(_localLinear, linear, _velocitySmoothing);
                    _localAngular = Vector3.Lerp(_localAngular, angular, _velocitySmoothing);
                }
            }

            _prevPosition = pose.position;
            _prevRotation = pose.rotation;
            _prevScale = scale;
            _prevTime = time;
            _hasPrevSample = true;
        }

        private float ScaleSuppression(Vector3 previousScale, Vector3 currentScale, float dt)
        {
            if (!_suppressVelocityWhileScaling || _scaleSuppressionRate <= 0f) return 1f;

            var prevMag = previousScale.magnitude;
            if (prevMag < 1e-6f) return 1f;

            var fractionalRate = Mathf.Abs(currentScale.magnitude - prevMag) / prevMag / dt;
            return Mathf.Clamp01(1f - fractionalRate / _scaleSuppressionRate);
        }

        private void ApplyAxisConstraints()
        {
            if (!_posX) _localLinear.x = 0;
            if (!_posY) _localLinear.y = 0;
            if (!_posZ) _localLinear.z = 0;

            if (!_rotX) _localAngular.x = 0;
            if (!_rotY) _localAngular.y = 0;
            if (!_rotZ) _localAngular.z = 0;

            if (_localLinear.sqrMagnitude > _maxLinear * _maxLinear)
                _localLinear = Vector3.ClampMagnitude(_localLinear, _maxLinear);

            if (_localAngular.sqrMagnitude > _maxAngular * _maxAngular)
                _localAngular = Vector3.ClampMagnitude(_localAngular, _maxAngular);
        }

        private void Update()
        {
            if (!_applyInertia) return;

            _localLinear = Vector3.Lerp(_localLinear, Vector3.zero, Time.deltaTime * _linearDrag);
            _localAngular = Vector3.Lerp(_localAngular, Vector3.zero, Time.deltaTime * _angularDrag);

            if (_localLinear.sqrMagnitude < 0.001f && _localAngular.sqrMagnitude < 0.001f)
            {
                _applyInertia = false;
                return;
            }

            _grabbable.Transform.localPosition += _localLinear * Time.deltaTime;
            if (_rotPivot == null) _grabbable.Transform.localRotation *= Quaternion.Euler(_localAngular * Time.deltaTime);
            else
            {
                var worldRot = _grabbable.Transform.rotation;
                var worldDeltaRot = worldRot * Quaternion.Euler(_localAngular * Time.deltaTime) * Quaternion.Inverse(worldRot);

                var offset = _grabbable.Transform.position - _rotPivot.position;

                _grabbable.Transform.position = _rotPivot.position + (worldDeltaRot * offset);
                _grabbable.Transform.localRotation *= worldDeltaRot;
            }

            var t = _grabbable.Transform;
            t.localPosition = Vector3.MoveTowards(Vector3.zero, t.localPosition,
                _maxDistance * t.localScale.x * t.localScale.x);
        }
    }
}
