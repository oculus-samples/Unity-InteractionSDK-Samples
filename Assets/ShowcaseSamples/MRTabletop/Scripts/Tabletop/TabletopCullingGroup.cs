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
using System.Linq;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TabletopCullingGroup : MonoBehaviour
    {
        [SerializeField]
        private ClipBoundsController _clipBounds;


        private CullingGroup _group;
        private BoundingSphere[] _bounds;
        private CullTarget[] _targets = Array.Empty<CullTarget>();
        private ScaleChangedNotifier _scaleChanged;

        public int _debugIndex = -1;
        public Transform _debugTarget;

        public bool MasterVisibility { get; private set; } = true;
        private bool _isDirty;


        private void Awake()
        {
            _scaleChanged = GetComponentInParent<ScaleChangedNotifier>();
        }

        private void OnEnable()
        {
            if (_clipBounds == null)
            {
                enabled = false;
                return;
            }

            _group = new()
            {
                targetCamera = Camera.main,
            };

            PopulateFromChildren();

            if (_scaleChanged != null) _scaleChanged.WhenScaleChanged += HandleScaleChanged;
        }

        private void OnDisable()
        {
            if (_group != null)
            {
                _group.onStateChanged = null;
                _group.Dispose();
                _group = null;
            }

            if (_scaleChanged) _scaleChanged.WhenScaleChanged -= HandleScaleChanged;
        }

        public void SetMasterVisibility(bool show)
        {
            if (MasterVisibility == show) return;
            MasterVisibility = show;
            _isDirty = true;
        }

        private void HandleScaleChanged()
        {
            if (_targets.Length == 0 || _bounds == null) return;

            for (int i = 0; i < _targets.Length; i++)
            {
                CullTarget target = _targets[i];
                target.UpdateBounds();
                _bounds[i].radius = target.BoundingRadius;
            }
        }


        private void LateUpdate()
        {
            if (_targets.Length == 0) return;

            if (_isDirty)
            {
                _isDirty = false;
                for (int i = 0; i < _targets.Length; i++)
                {
                    _targets[i].MasterVisible = MasterVisibility;
                    _targets[i].UpdateRenderState();
                }

                return;
            }

            if (!MasterVisibility) return;

            for (int i = 0; i < _targets.Length; i++)
            {
                CullTarget target = _targets[i];

                var worldCenter = target.WorldCenter;
                bool withinBounds = _clipBounds.PointWithinClip(worldCenter, target.BoundingRadius);
                if (withinBounds) _bounds[i].position = worldCenter;

                if (target.WithinBounds != withinBounds)
                {
                    target.WithinBounds = withinBounds;
                    target.UpdateRenderState();
                }
            }
        }




        private void PopulateFromChildren()
        {
            List<CullTarget> tempTargets = new List<CullTarget>();
            HashSet<Renderer> allRenderers = new HashSet<Renderer>(GetComponentsInChildren<Renderer>(true));


            Grabbable[] grabbables = GetComponentsInChildren<Grabbable>(true);
            foreach (Grabbable grab in grabbables)
            {
                List<Renderer> childRenderers = new();
                allRenderers.RemoveWhere(x =>
                {
                    if (!x.transform.IsChildOf(grab.transform)) return false;

                    childRenderers.Add(x);
                    return true;
                });

                HandGrabInteractable[] grabs = grab.GetComponentsInChildren<HandGrabInteractable>(true);
                Behaviour[] behaviours = grabs.Select(x => x as Behaviour).ToArray();

                CullTarget target = new CullTarget(grab.gameObject, childRenderers)
                {
                    DisablesTarget = false,
                    Behaviours = behaviours,
                };
                tempTargets.Add(target);
            }


            foreach (Renderer leftover in allRenderers) tempTargets.Add(new CullTarget(leftover));
            _targets = tempTargets.ToArray();

            RebuildCullingGroup();
        }

        private void RebuildCullingGroup()
        {
            if (_group == null) return;

            _bounds = new BoundingSphere[_targets.Length];

            for (int i = 0; i < _targets.Length; i++)
            {
                CullTarget target = _targets[i];
                _bounds[i] = new BoundingSphere(target.WorldCenter, target.BoundingRadius);
            }

            _group.SetBoundingSpheres(_bounds);
            _group.SetBoundingSphereCount(_targets.Length);
        }

        [MetaCodeSample("ISDKSamples-ShowcaseSamples")]
        private class CullTarget
        {
            public GameObject Target;
            public Renderer[] Renderers;

            private Renderer[] _boundsRenderers;
            private Behaviour[] _behaviours;

            public Behaviour[] Behaviours
            {
                get => _behaviours;
                set
                {
                    _behaviours = value;
                    _hasBehaviours = _behaviours != null && _behaviours.Length > 0;
                }
            }

            public bool MasterVisible = true;
            public bool VisibleByCamera = true;
            public bool WithinBounds = true;

            private bool _prevVisibility = true;

            public bool DisablesTarget = true;
            public Vector3 LocalCenterOffset;
            public float BoundingRadius;

            private readonly Transform _transform;

            //UseLocalOffset should just be, if encapsulated bounds.center is different from _target.position; Calculated once?
            private bool _useLocalOffset;
            private bool _hasBehaviours;

            public Vector3 WorldCenter => _useLocalOffset ? _transform.TransformPoint(LocalCenterOffset) : _transform.position;

            public CullTarget(GameObject target, List<Renderer> renderers)
            {
                Target = target;
                Renderers = renderers.ToArray();

                var hasTarget = target != null;
                _transform = hasTarget ? target.transform : Renderers[0].transform;

                _boundsRenderers = renderers.Where(x => x is not ParticleSystemRenderer).ToArray();
                UpdateBounds();
            }


            public CullTarget(Renderer renderer)
            {
                Renderers = new[] { renderer };
                _transform = renderer.transform;
                _useLocalOffset = true;

                _boundsRenderers = renderer is ParticleSystemRenderer ? Array.Empty<Renderer>() : Renderers;

                UpdateBounds();
            }

            public void UpdateBounds()
            {
                if (_boundsRenderers == null || _boundsRenderers.Length == 0)
                {
                    LocalCenterOffset = Vector3.zero;
                    BoundingRadius = 0.001f;
                    return;
                }

                Bounds bounds = _boundsRenderers[0].bounds;
                for (int i = 1; i < _boundsRenderers.Length; i++) bounds.Encapsulate(_boundsRenderers[i].bounds);

                LocalCenterOffset = _transform != null ? _transform.InverseTransformPoint(bounds.center) : Vector3.zero;
                BoundingRadius = bounds.extents.magnitude * 1.2f;

                _useLocalOffset = LocalCenterOffset.sqrMagnitude > 0.0001f;
            }

            public void UpdateRenderState()
            {
                bool shouldBeVisible = MasterVisible && VisibleByCamera && WithinBounds;
                if (_prevVisibility == shouldBeVisible) return;
                _prevVisibility = shouldBeVisible;

                foreach (Renderer rend in Renderers)
                {
                    if (rend) rend.forceRenderingOff = !shouldBeVisible;
                }

                if (DisablesTarget && Target)
                {
                    Target.SetActive(shouldBeVisible);
                }

                if (_hasBehaviours)
                {
                    foreach (Behaviour behaviour in Behaviours)
                    {
                        if (behaviour) behaviour.enabled = shouldBeVisible;
                    }
                }
            }


            public void DrawBounds()
            {
                Bounds bounds = _boundsRenderers[0].bounds;
                for (int i = 1; i < _boundsRenderers.Length; i++) bounds.Encapsulate(_boundsRenderers[i].bounds);

            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            _debugIndex = Mathf.Clamp(_debugIndex, -1, _targets.Length - 1);

            if (_debugIndex < 0)
            {

                if (_debugTarget != null)
                {
                    var index = _targets.Select(x => x.Target != null ? x.Target.transform : x.Renderers[0]?.transform).ToList().IndexOf(_debugTarget);
                    _debugIndex = index;
                }
                return;
            }


            var target = _targets[_debugIndex];
            _debugTarget = target.Target != null
                ? target.Target.transform
                : target.Renderers[0]?.transform;
            var bounds = _bounds[_debugIndex];

            var scale = transform.lossyScale.x * 0.1f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(target.WorldCenter, scale);
            Gizmos.DrawWireSphere(target.WorldCenter, target.BoundingRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(bounds.position, scale);
            Gizmos.DrawWireSphere(bounds.position, bounds.radius);
        }
    }
}
