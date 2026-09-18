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
using UnityEngine;
using UnityEngine.Rendering;

namespace Meta.XR.InteractionSDK.Samples
{
    [ExecuteAlways]
    [MetaCodeSample("ISDK-Tabletop")]
    public class AutoLightScaler : MonoBehaviour
    {
        private float _setupIntensity = 0.8f, _setupRange = 2f;

        private Light _light;
        private bool _isScaled;

        private void OnEnable()
        {
            _light = GetComponent<Light>();

            // SRP (URP/HDRP) callbacks
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRenderingSRP;
            RenderPipelineManager.endCameraRendering += OnEndCameraRenderingSRP;

            // BIRP callbacks - Camera static events work in Built-in RP,
            // and are no-ops filtered when SRP is active
            Camera.onPreCull += OnPreCullBIRP;
            Camera.onPostRender += OnPostRenderBIRP;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRenderingSRP;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRenderingSRP;

            Camera.onPreCull -= OnPreCullBIRP;
            Camera.onPostRender -= OnPostRenderBIRP;

            // Safety revert if disabled mid-frame while scaled
            if (_isScaled)
            {
                RevertLightInternal();
            }
        }

        // ---- SRP ----

        private void OnBeginCameraRenderingSRP(ScriptableRenderContext context, Camera camera)
        {
            ScaleLightInternal();
        }

        private void OnEndCameraRenderingSRP(ScriptableRenderContext context, Camera camera)
        {
            RevertLightInternal();
        }

        // ---- BIRP (Built-in Render Pipeline) ----

        private void OnPreCullBIRP(Camera camera)
        {
            // When SRP is active, Camera events may still fire for compatibility.
            // Skip BIRP path in that case to avoid double-scaling - SRP handles it.
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                return;
            }

            ScaleLightInternal();
        }

        private void OnPostRenderBIRP(Camera camera)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                return;
            }

            RevertLightInternal();
        }

        // ---- Core logic ----

        private void ScaleLightInternal()
        {
            if (_isScaled)
            {
                return;
            }

            if (_light == null)
            {
                _light = GetComponent<Light>();
                if (_light == null)
                {
                    return;
                }
            }


            _setupIntensity = _light.intensity;
            _setupRange = _light.range;

            var scale = transform.lossyScale.x;
            var ratio = scale;
            _light.range = _setupRange * ratio;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                _light.intensity = Mathf.Max(0.001f, _setupIntensity * ratio * ratio);
            }

            _isScaled = true;
        }

        private void RevertLightInternal()
        {
            if (!_isScaled)
            {
                return;
            }

            if (_light != null)
            {
                _light.intensity = _setupIntensity;
                _light.range = _setupRange;
            }

            _isScaled = false;
        }
    }
}
