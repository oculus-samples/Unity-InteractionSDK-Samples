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

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(LightController))]
    [MetaCodeSample("ISDK-Tabletop")]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField]
        private float _minMultiplier = 0.5f;

        [SerializeField]
        private float _maxMultiplier = 1.1f;

        [SerializeField]
        private float _speed = 5f;

        private LightController _lightController;

        private float _offset;

        private void Awake()
        {
            _lightController = GetComponent<LightController>();
        }

        private void Start()
        {
            _offset = Random.value * 1000f;
        }

        private void Update()
        {
            var noise = Mathf.PerlinNoise(Time.time * _speed, _offset);
            var mult = Mathf.Lerp(_minMultiplier, _maxMultiplier, noise);
            _lightController.SetIntensityMultiplier(this, mult);
        }

        private void OnDisable()
        {
            if (_lightController) _lightController.RemoveMultipliersForSource(this);
        }
    }
}
