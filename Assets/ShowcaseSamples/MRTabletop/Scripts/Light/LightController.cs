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
using Meta.XR.Samples;
using System;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(Light))]
    [MetaCodeSample("ISDK-Tabletop")]
    public class LightController : MonoBehaviour
    {
        public float BaseIntensity { get; set; } = 1f;
        public float BaseRange { get; set; } = 1f;

        private Light _light;
        private Dictionary<object, Multipliers> _multipliers = new();

        public bool IsActive => _light && _light.enabled;

        //Whether the light is visible, e.g. disabled or light.intensity == 0. 
        public bool LightVisible { get; private set; }
        public event Action WhenLightVisibilityChanged;

        private void Awake()
        {
            _light = GetComponent<Light>();
            BaseIntensity = _light.intensity;
            BaseRange = _light.range;
        }

        public void SetActive(bool active)
        {
            if (!_light) _light = GetComponent<Light>();
            if (_light.enabled == active) return;

            _light.enabled = active;
            UpdateLightVisibility();
        }

        private void UpdateLightVisibility()
        {
            var visible = _light.enabled && _light.intensity > 0.001f;
            if (visible == LightVisible) return;
            LightVisible = visible;
            WhenLightVisibilityChanged?.Invoke();
        }

        public void SetIntensityMultiplier(object source, float multiplier)
        {
            EnsureMultiplier(source);
            _multipliers[source].Intensity = multiplier;
        }

        public void SetRangeMultiplier(object source, float multiplier)
        {
            EnsureMultiplier(source);
            _multipliers[source].Range = multiplier;
        }

        public void RemoveMultipliersForSource(object source)
        {
            _multipliers.Remove(source);
        }

        private void EnsureMultiplier(object source)
        {
            if (!_multipliers.ContainsKey(source)) _multipliers.Add(source, new());
        }

        private void LateUpdate()
        {
            var finalIntensity = BaseIntensity;
            var finalRange = BaseRange;
            foreach (var mult in _multipliers.Values)
            {
                finalIntensity *= mult.Intensity;
                finalRange *= mult.Range;
            }

            if (!Mathf.Approximately(_light.intensity, finalIntensity))
            {
                UpdateLightVisibility();
            }

            _light.intensity = finalIntensity;
            _light.range = finalRange;
        }

        [MetaCodeSample("ISDKSamples-ShowcaseSamples")]
        public class Multipliers
        {
            public float Intensity = 1;
            public float Range = 1;
        }
    }
}
