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
    [MetaCodeSample("ISDK-Tabletop")]
    public class FacePlayer : MonoBehaviour
    {
        [SerializeField]
        private float _transitionSpeed = 5f;
        [SerializeField]
        private float _maxDegreesDelta = 10f;

        [SerializeField]
        private bool _updateInstantOnRecenter;

        private void OnEnable()
        {
            if (_updateInstantOnRecenter && OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose += UpdateInstant;
            }
        }

        private void OnDisable()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= UpdateInstant;
        }

        private void UpdateInstant() => UpdateLookAt(true);


        private void UpdateLookAt(bool instant = false)
        {
            var cam = Camera.main;
            if (!cam) return;

            var toDir = cam.transform.position - transform.position;
            toDir.y = 0;
            var targetRot = Quaternion.RotateTowards(Quaternion.LookRotation(toDir), transform.rotation, _maxDegreesDelta);

            transform.rotation = instant
                ? targetRot
                : Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _transitionSpeed);
        }

        private void Update()
        {
            UpdateLookAt();
        }
    }
}
