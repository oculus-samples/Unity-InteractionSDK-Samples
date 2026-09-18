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
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class IsInViewActiveState : MonoBehaviour, IActiveState, IComparable<IsInViewActiveState>
    {
        [SerializeField]
        private bool _worldSpace = true;

        [SerializeField, Tooltip("The radius of the object, a large object is more likely to be in view")]
        private float _radius = 0f;

        [SerializeField, Tooltip("The FOV of the player, smaller numbers will require a more direct view, -1 uses the camera's fov")]
        private float _overrideFov = -1f;

        [SerializeField, Tooltip("The minimum distance to consider in view, objects that are closer to the player are not in view")]
        private float _minDistance = -1f;

        [SerializeField, Tooltip("The max distance to that player, if the object is further than this its not in view")]
        private float _maxDistance = -1f;

        [SerializeField]
        private Vector3 _cameraRotationOffset = Vector3.zero;

        private static Camera _mainCamera;

        private bool _lastActive;

        [SerializeField, Tooltip("Additional fov for exit buffering. To prevent rapid Active -> Inactive switching at boundary."), Min(0)]
        private float _exitBufferFov = 0;

        private float Scalar => _worldSpace ? 1f : Mathf.Abs(transform.lossyScale.x);
        private float Radius => _radius * Scalar;
        private float MinDistance => _minDistance > 0 ? _minDistance * Scalar : _minDistance;
        private float MaxDistance => _maxDistance > 0 ? _maxDistance * Scalar : _maxDistance;

        public bool Active
        {
            get
            {
                var active = IsInFOV(out _);
                _lastActive = active;
                return active;
            }
        }

        private bool IsInFOV(out float angle) => IsInFOV(transform.position, out angle, Radius, MaxDistance, MinDistance, _overrideFov, _cameraRotationOffset);

        public bool IsInFOV(Vector3 position, out float angle, float radius = 0, float maxDistance = -1, float minDistance = -1, float fov = -1, Vector3 cameraRotationOffset = default)
        {
            angle = -1;
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return false;
            }

            var viewPose = _mainCamera.transform.GetPose();

            var toThis = position - viewPose.position;
            var distance = toThis.magnitude;
            if (maxDistance > 0 && distance - radius > maxDistance) return false;
            if (minDistance > 0 && distance < minDistance) return false;

            var radiusToAngle = Mathf.Atan2((float)radius, distance) * Mathf.Rad2Deg;
            fov = (fov > 0 ? fov : _mainCamera.fieldOfView);
            if (_lastActive) fov += _exitBufferFov;

            var apature = radiusToAngle + fov * 0.5f;

            Quaternion offset = Quaternion.Euler(cameraRotationOffset);
            Vector3 forward = viewPose.rotation * (offset * Vector3.forward);
            angle = Vector3.Angle(forward, toThis);
            return angle < apature;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsInFOV(out var _) ? Color.green : Color.white;
            Gizmos.DrawWireSphere(transform.position, Radius);

            if (_mainCamera)
            {
                Gizmos.matrix = Matrix4x4.TRS(_mainCamera.transform.position, _mainCamera.transform.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, _overrideFov > 0 ? _overrideFov : _mainCamera.fieldOfView, MaxDistance > 0 ? MaxDistance : 100, MinDistance > 0 ? MinDistance : 0, 1);
            }
        }

        public int CompareTo(IsInViewActiveState other)
        {
            var thisInView = IsInFOV(out var thisAngle);
            var otherInView = IsInFOV(out var otherAngle);

            if (thisInView != otherInView) return thisInView ? -1 : 1;
            else return thisAngle.CompareTo(otherAngle);
        }
    }
}
