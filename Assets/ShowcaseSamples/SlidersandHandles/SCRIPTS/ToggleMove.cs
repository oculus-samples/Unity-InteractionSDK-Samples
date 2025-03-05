/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Meta Platform Technologies SDK License Agreement (the “SDK License”).
 * You may not use the MPT SDK except in compliance with the SDK License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the SDK License at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the MPT SDK
 * distributed under the SDK License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the SDK License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    public class ToggleMove : MonoBehaviour
    {
        [SerializeField] private GameObject _onLabel;
        [SerializeField] private GameObject _offLabel;
        [SerializeField] private AudioSource _click;
        [SerializeField] private GameObject _toggleHandle;

        private bool _isMoving;
        private bool _isOn;

        void Start()
        {
            CheckStatus();
        }

        private void Update()
        {
            if (_isMoving)
            {
                CheckStatus();
            }
            else
            {
                SnapToPlace();
            }
        }

        private void CheckStatus()
        {
            if (_toggleHandle.transform.localPosition.x > 0)
            {
                if (!_isOn)
                {
                    _isOn = true;
                    ToggleVisuals(true);
                    _click.Play();
                }
            }
            else
            {
                if (_isOn)
                {
                    _isOn = false;
                    ToggleVisuals(false);
                    _click.Play();
                }
            }
        }

        private void SnapToPlace()
        {

            if (_isOn)
            {
                _toggleHandle.transform.localPosition = new Vector3(0.06f, 0, 0);
                ToggleVisuals(false);
            }
            else
            {
                _toggleHandle.transform.localPosition = new Vector3(-0.06f, 0, 0);
                ToggleVisuals(true);
            }
        }

        public void SetMoving(bool setMoving)
        {
            _isMoving = setMoving;
        }

        private void ToggleVisuals(bool state)
        {
            if (_onLabel)
            {
                _onLabel.SetActive(state);
            }

            if (_offLabel)
            {
                _offLabel.SetActive(!state);
            }
        }
    }
}
