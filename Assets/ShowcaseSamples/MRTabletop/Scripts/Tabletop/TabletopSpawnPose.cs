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
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TabletopSpawnPose : MonoBehaviour
    {
        [SerializeField]
        private Transform _target;
        [SerializeField]
        private Vector3 _posOffset;

        private Transform _centerEyeAnchor;

        private void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (!rig)
            {
                Debug.LogError($"Could not find an OVRCameraRig. Disabling component.");
                enabled = false;
                return;
            }

            _centerEyeAnchor = rig.centerEyeAnchor;
            StartCoroutine(EnsurePositionRoutine());
        }

        private IEnumerator EnsurePositionRoutine()
        {
            var initialPos = _centerEyeAnchor.localPosition;
            var timeout = 2.0f;
            var elapsed = 0f;

            while ((_centerEyeAnchor.localPosition - initialPos).sqrMagnitude < 0.0001 && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return null;
            UpdatePosition();
        }

        private void OnEnable()
        {
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose += UpdatePosition;
            }

            UpdatePosition();
        }


        private void OnDisable()
        {
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose -= UpdatePosition;
            }

            StopAllCoroutines();
        }

        [ContextMenu("UpdatePosition")]
        private void UpdatePosition()
        {
            if (!_centerEyeAnchor) return;
            var forward = Vector3.ProjectOnPlane(_centerEyeAnchor.forward, Vector3.up);
            var rot = Quaternion.LookRotation(forward, Vector3.up);
            var offset = rot * _posOffset;

            _target.position = _centerEyeAnchor.position + offset;
            _target.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        }
    }
}
