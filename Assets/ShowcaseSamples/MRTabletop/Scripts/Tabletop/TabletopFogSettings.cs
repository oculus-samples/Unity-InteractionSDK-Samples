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

using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TabletopFogSettings : MonoBehaviour
    {
        public float Strength = 0;
        public float Height = 1;
        public Color Color = Color.white;

        [Optional]
        public Transform Pivot;

        public List<RendererList> RendererGroups;
        public List<Renderer> Renderers;


        private int _fogProp, _fogColorProp;

        private float _lastStrength;
        private float _lastHeight;
        private Color _lastColor;
        private float _lastPivotYPos;
        private float _lastPivotYScale;

        private List<Material> _instancedMaterials = new();
        private Dictionary<Material, Material> _matMap = new();

        private void Awake()
        {
            SetupMaterials();
        }

        private void OnEnable()
        {
            if (!Pivot) Pivot = transform;

            _fogProp = Shader.PropertyToID("_VerticalFog");
            _fogColorProp = Shader.PropertyToID("_VerticalFogColor");

            _lastStrength = -Strength; //ForceUpdate.

        }

        private void SetupMaterials()
        {
            HashSet<Renderer> allRenderers = new HashSet<Renderer>(Renderers);
            foreach (var group in RendererGroups)
            {
                if (group != null && group.Renderers != null)
                {
                    allRenderers.UnionWith(group.Renderers);
                }
            }

            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;

                Material[] sharedMats = renderer.sharedMaterials;
                var changed = false;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material original = sharedMats[i];

                    if (!_matMap.TryGetValue(original, out Material matInstance))
                    {
                        matInstance = new Material(original);
                        matInstance.name = original.name + "_fogInstance" + gameObject.name;

                        _matMap[original] = matInstance;
                        _instancedMaterials.Add(matInstance);
                    }

                    sharedMats[i] = matInstance;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = sharedMats;
                }
            }
        }

        private void Update()
        {
            if (IsDirty()) ApplySettings();
        }

        private void ApplySettings()
        {
            var fog = new Vector4(Pivot.position.y, Height * Pivot.lossyScale.y, Strength, 0);

            foreach (var mat in _instancedMaterials)
            {
                if (mat != null)
                {
                    mat.SetVector(_fogProp, fog);
                    mat.SetColor(_fogColorProp, Color);
                }
            }
        }


        private bool IsDirty()
        {
            var currentYPos = Pivot.position.y;
            var currentYScale = Pivot.lossyScale.y;

            if (!Mathf.Approximately(Strength, _lastStrength) ||
                !Mathf.Approximately(Height, _lastHeight) ||
                Color != _lastColor ||
                !Mathf.Approximately(currentYPos, _lastPivotYPos) ||
                !Mathf.Approximately(currentYScale, _lastPivotYScale))
            {

                _lastStrength = Strength;
                _lastHeight = Height;
                _lastColor = Color;
                _lastPivotYPos = currentYPos;
                _lastPivotYScale = currentYScale;
                return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            foreach (var mat in _matMap.Values)
            {
                if (mat != null) Destroy(mat);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            FindRenderers();
        }

        [ContextMenu("FindRenderers")]
        private void FindRenderers()
        {
            HashSet<Renderer> renderers = new HashSet<Renderer>();
            renderers.UnionWith(GetComponentsInChildren<Renderer>(true));
            renderers.RemoveWhere(x => x is ParticleSystemRenderer);
            foreach (var group in RendererGroups) renderers = renderers.Except(group.Renderers).ToHashSet();

            Renderers = renderers.ToList();
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("ToggleTest")]
        private void ToggleTest()
        {
            foreach (var renderer in Renderers)
            {
                if(renderer) renderer.enabled = !renderer.enabled;
            }

            foreach (var group in RendererGroups) group.ToggleTest();

        }
#endif
    }
}
