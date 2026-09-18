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
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.XR.InteractionSDK.Samples
{
    /// <summary>
    /// This script defines the visible volume of a game space.
    /// All objects using the "Custom/AreaClip" shader will be clipped to this volume.
    /// The bounding object can be moved, rotated, and scaled at runtime.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [MetaCodeSample("ISDK-Tabletop")]
    public class ClipBoundsController : MonoBehaviour
    {
        public enum ClipMode
        {
            Box = 0,
            Sphere = 1,
            Cylinder = 2,
        }

        [Tooltip("Box: uses full transform (position, rotation, scale)\nSphere: uses position and scale (rotation ignored)")]
        public ClipMode _clipMode = ClipMode.Box;

        [Tooltip("Distance in world units for edge fade (0 = hard edge)")]
        [Range(0f, 0.5f)]
        public float _fadeWidth = 0.05f;

        [Tooltip("Hide this bounding object's renderer at start")]
        public bool _hideBoundsRenderer = true;

        [Header("Bounds")]
        [Header("Content (optional)")]
        [Tooltip("Root of geometry that should be clipped (e.g. MoveableObject). If set, materials are upgraded to clip-capable shaders at runtime.")]
        [SerializeField] private Transform clipContentRoot;

        private static readonly int ClipCenterID = Shader.PropertyToID("_ClipCenter");
        private static readonly int ClipSizeID = Shader.PropertyToID("_ClipSize");
        private static readonly int ClipWorldToLocalID = Shader.PropertyToID("_ClipWorldToLocal");
        private static readonly int ClipModeID = Shader.PropertyToID("_ClipMode");
        private static readonly int ClipFadeWidthID = Shader.PropertyToID("_ClipFadeWidth");

        private Transform _transform;
        private Matrix4x4 _cachedWorldToLocal;
        private Vector3 _cachedLossyScale;
        private Vector3 _cachedAbsScale;
        private Vector3 _cachedPosition;

        private Matrix4x4 _lastMatrix;
        private Vector3 _lastScale;
        private ClipMode _lastClipMode;
        private float _lastFadeWidth;

        private void Awake()
        {
            _transform = transform;
            UpdateCache();
        }

        private void Start()
        {
            if (_hideBoundsRenderer && TryGetComponent(out Renderer rend))
            {
                rend.enabled = false;
            }
        }


        private void UpdateCache()
        {
            _cachedWorldToLocal = _transform.worldToLocalMatrix;
            _cachedLossyScale = _transform.lossyScale;
            _cachedPosition = _transform.position;
            _cachedAbsScale = new Vector3(Mathf.Abs(_cachedLossyScale.x), Mathf.Abs(_cachedLossyScale.y), Mathf.Abs(_cachedLossyScale.z));
        }

        private void LateUpdate()
        {
            UpdateCache();

            bool transformChanged = _cachedWorldToLocal != _lastMatrix || _cachedLossyScale != _lastScale;
            bool settingsChanged = _clipMode != _lastClipMode || !Mathf.Approximately(_fadeWidth, _lastFadeWidth);

            if (transformChanged || settingsChanged)
            {
                _lastMatrix = _cachedWorldToLocal;
                _lastScale = _cachedLossyScale;
                _lastClipMode = _clipMode;
                _lastFadeWidth = _fadeWidth;

                UpdateShaderProperties();
            }
        }

        private void OnDestroy()
        {
            Shader.SetGlobalInt(ClipModeID, 0);
        }

        public bool PointWithinClip(Vector3 worldPoint, float padding = 0f)
        {
            Vector3 localPos = _cachedWorldToLocal.MultiplyPoint3x4(worldPoint);
            Vector3 absScale = _cachedAbsScale;

            switch (_clipMode)
            {
                case ClipMode.Box:
                {
                    float absX = localPos.x >= 0 ? localPos.x : -localPos.x;
                    if (absX > 0.5f + (padding / absScale.x)) return false;

                    float absY = localPos.y >= 0 ? localPos.y : -localPos.y;
                    if (absY > 0.5f + (padding / absScale.y)) return false;

                    float absZ = localPos.z >= 0 ? localPos.z : -localPos.z;
                    if (absZ > 0.5f + (padding / absScale.z)) return false;

                    return true;
                }
                case ClipMode.Sphere:
                {
                    float avgScale = (absScale.x + absScale.y + absScale.z) * 0.3333333f;
                    float maxDist = 0.5f + (padding / avgScale);

                    return localPos.sqrMagnitude <= (maxDist * maxDist);
                }
                case ClipMode.Cylinder:
                {
                    float absY = localPos.y >= 0 ? localPos.y : -localPos.y;
                    if (absY > 0.5f + (padding / absScale.y)) return false;

                    float sqrDistXZ = localPos.x * localPos.x + localPos.z * localPos.z;
                    float avgScaleXZ = (absScale.x + absScale.z) * 0.5f;
                    float maxDistXZ = 0.5f + (padding / avgScaleXZ);
                    return sqrDistXZ <= (maxDistXZ * maxDistXZ);
                }
            }

            return false;
        }


        [ContextMenu("UpdateShaderProperties")]
        void UpdateShaderProperties()
        {
            // Set global shader properties (affects all materials using the AreaClip shader)
            Shader.SetGlobalVector(ClipCenterID, _cachedPosition);
            Shader.SetGlobalVector(ClipSizeID, _cachedLossyScale);
            Shader.SetGlobalMatrix(ClipWorldToLocalID, _cachedWorldToLocal);
            Shader.SetGlobalInt(ClipModeID, (int)_clipMode + 1);
            Shader.SetGlobalFloat(ClipFadeWidthID, _fadeWidth);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            UpdateShaderProperties();
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            Handles.color = Gizmos.color;
            Handles.matrix = transform.localToWorldMatrix;

            switch (_clipMode)
            {
                case ClipMode.Box:
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
                case ClipMode.Sphere:
                    Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                    break;
                case ClipMode.Cylinder:
                    DrawWireCylinder(Vector3.zero, 0.5f, 1f);
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;

            Handles.color = Gizmos.color;
            Handles.matrix = transform.localToWorldMatrix;

            switch (_clipMode)
            {
                case ClipMode.Box:
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
                case ClipMode.Sphere:
                    Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                    break;
                case ClipMode.Cylinder:
                    DrawWireCylinder(Vector3.zero, 0.5f, 1f);
                    break;
            }
        }


        private void DrawWireCylinder(Vector3 center, float radius, float height)
        {
            Vector3 top = center + Vector3.up * height * 0.5f;
            Vector3 bottom = center - Vector3.up * height * 0.5f;

            Handles.DrawWireDisc(top, Vector3.up, radius);
            Handles.DrawWireDisc(bottom, Vector3.up, radius);

            Vector3 forward = Vector3.forward * radius;
            Vector3 right = Vector3.right * radius;

            Gizmos.DrawLine(top + forward, bottom + forward);
            Gizmos.DrawLine(top - forward, bottom - forward);
            Gizmos.DrawLine(top + right, bottom + right);
            Gizmos.DrawLine(top - right, bottom - right);
        }
#endif
    }
}
