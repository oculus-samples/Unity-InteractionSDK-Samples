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
    public class ButtonToggleVisuals : MonoBehaviour
    {
        [SerializeField] GameObject _onVisual;
        [SerializeField] GameObject _offVisual;

        private bool _isOn;

        void Start()
        {
            ToggleButton();
        }

        private void ToggleButton()
        {
            _isOn = !_isOn;

            _onVisual.SetActive(_isOn);
            _offVisual.SetActive(!_isOn);
        }
    }
}
