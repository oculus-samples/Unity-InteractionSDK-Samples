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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class LightStagger : MonoBehaviour
    {
        [SerializeField]
        private Transform _staggerFrom;

        [SerializeField]
        private List<LightController> _lightControllers;

        [SerializeField]
        private float _duration = 0.2f;

        [SerializeField]
        private float _delay = 0.05f;

        private LightController[] _orderedLights;

        private void Start()
        {
            _orderedLights = _lightControllers.OrderBy(x => (_staggerFrom.position - x.transform.position).sqrMagnitude).ToArray();
            foreach (var controller in _orderedLights)
            {
                controller.SetRangeMultiplier(this, 0f);
                controller.SetIntensityMultiplier(this, 0f);
            }
        }

        public void StaggerLights()
        {
            StopAllCoroutines();
            StartCoroutine(StaggerLightsRoutine());
        }


        private IEnumerator StaggerLightsRoutine()
        {
            var wait = new WaitForSeconds(_delay);
            foreach (var controller in _orderedLights)
            {
                controller.SetRangeMultiplier(this, 0f);
                controller.SetIntensityMultiplier(this, 0f);
            }

            foreach (var controller in _orderedLights)
            {
                var elapsed = 0f;
                while (elapsed < _duration)
                {
                    elapsed += Time.deltaTime;
                    var mult = Mathf.Clamp01(elapsed / _duration);

                    controller.SetIntensityMultiplier(this, mult);
                    controller.SetRangeMultiplier(this, mult);
                    yield return null;
                }

                controller.RemoveMultipliersForSource(this);
                yield return wait;
            }
        }

        private void Reset()
        {
            _lightControllers = GetComponentsInChildren<LightController>().ToList();
        }
    }
}
